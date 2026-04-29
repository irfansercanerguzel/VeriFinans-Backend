using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VeriFinans.Models;
using System.Net.Http;

namespace VeriFinans.Services
{
    public class AiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        // Sorunsuz, yüksek kotalı endpoint
        private const string GeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite-preview:generateContent";

        public AiService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"]
                      ?? throw new InvalidOperationException("Gemini API Key yapılandırma dosyasında bulunamadı.");

            _httpClient = new HttpClient();
        }

        // --- ESKİ METOT 1: AnalyzeStatementAsync (Aynen Korundu) ---
        public async Task<string> AnalyzeStatementAsync(List<Expense> localExpenses, byte[] pdfBytes)
        {
            try
            {
                var localDataSummary = localExpenses.Select(e => new {
                    Tarih = e.Date.ToString("dd.MM.yyyy"),
                    Aciklama = e.Description,
                    Tutar = $"{e.Amount} TL"
                }).ToList();

                string localJson = JsonConvert.SerializeObject(localDataSummary, Formatting.Indented);
                string base64Pdf = Convert.ToBase64String(pdfBytes);

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = $@"Aşağıdaki JSON verileri kullanıcının sistemdeki kayıtlı harcamalarıdır. 
Ekteki PDF ise banka ekstresidir. Lütfen bu iki kaynağı karşılaştırarak:
1. Kayıtlarda olup ekstrede olmayan,
2. Ekstrede olup kayıtlarda olmayan harcamaları tespit et.
3. Aradaki farkları içeren, profesyonel ve anlaşılır bir finansal analiz raporu hazırla.

Kullanıcı Kayıtları:{localJson}" },
                                new { inline_data = new { mime_type = "application/pdf", data = base64Pdf } }
                            }
                        }
                    }
                };

                var jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GeminiEndpoint}?key={_apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    dynamic? result = JsonConvert.DeserializeObject(responseJson);

                    string? aiText = result?.candidates?[0]?.content?.parts?[0]?.text;
                    return aiText ?? "Analiz işlemi tamamlandı ancak anlamlı bir metin üretilemedi.";
                }

                var errorBody = await response.Content.ReadAsStringAsync();
                return $"Servis Hatası ({response.StatusCode}): Analiz raporu şu an oluşturulamıyor. Detay: {errorBody}";
            }
            catch (Exception ex)
            {
                return $"Beklenmeyen bir hata oluştu: {ex.Message}";
            }
        }

        // --- ESKİ METOT 2: ExtractTransactionsAsync (Aynen Korundu) ---
        public async Task<List<Expense>> ExtractTransactionsAsync(byte[] fileBytes, string fileExtension, string text)
        {
            var parts = new List<object>();

            string categoryList = @"
KATEGORİ LİSTESİ VE ID'LERİ:
- İkamet & Faturalar: Aidat (20, 25, 36), Doğalgaz (21, 26, 37), Elektrik (22, 33, 38), Su (23, 34, 39), İnternet (24, 35, 40)
- Market / Alışveriş / E-Ticaret (Trendyol, Hepsiburada, Migros vb.): 4
- Kasap / Sadece Et Ürünleri: 5
- Eczane / Sağlık: 8
- Kediler / Petshop: 9
- Araba: Kasko (29, 41), Trafik Sig. (30, 42), MTV (31, 43), Benzin/Akaryakıt (32, 44)
- Telefon Faturaları: Sercan (15), Hakkı (16), Ayşem (17)
- DASK / Yangın Sigortaları: 27, 28
- DİĞER / NE OLDUĞU BİLİNMEYEN HARCAMALAR: 106
";

            string prompt = $@"Sen çok yetenekli ve zeki bir finansal veri analiz uzmanısın. Sana verilen banka ekstresi veya kopyalanmış metinden YENİ ALIŞVERİŞLERİ analiz edip aşağıdaki KESİN kurallara göre SADECE JSON ARRAY döndüreceksin.

ÖNEMLİ KURAL 1: Tutar (Amount) değerlerinde ondalık ayracı olarak virgül (,) değil, KESİNLİKLE nokta (.) kullanmalısın! (Örn: 316.34)
ÖNEMLİ KURAL 2: 'Önceki Hesap Özeti Bakiyesi', 'Ödeme - Teşekkürler', 'Hesaptan Aktarım' gibi borç ödemelerini KESİNLİKLE YOK SAY!
ÖNEMLİ KURAL 3 (TAKSİT DETAYINI KAÇIRMA): Eğer metinde işlem TAKSİTLİ ise (Örn: '1/3', '1/6' gibi ibareler varsa) 'InstallmentCount' değerine TOPLAM TAKSİT SAYISINI tam sayı olarak yaz (Örn: 3). Açıklamaya (Description) ise metinde gördüğün şekliyle kaçıncı taksit olduğunu MUTLAKA EKLE (Örn: 'WWW.TRENDYOL.COM (1/6 Taksit)'). Taksit yoksa InstallmentCount kesinlikle 1 olmalı.
ÖNEMLİ KURAL 4 (MÜKERRER KONTROLÜ - İSMİN CANI CEHENNEME): Bir kaydın çift (kopyalama hatası) olup olmadığına karar verirken İSME KESİNLİKLE BAKMA! Sadece Tarih ve Tutara bakacaksın. MANTIK ŞU: 
- Tarih ve Tutarın HER İKİSİ BİRDEN kuruşu kuruşuna aynıysa: Kopyalama hatasıdır, SADECE 1 TANESİNİ EKLE.
- Tarih FARKLIYSA VEYA Tutar FARKLIYSA: İsimleri tamamen aynı bile olsa BUNLAR FARKLI HARCAMALARDIR, ikisini de ayrı ayrı listeye EKLE!
ÖNEMLİ KURAL 5 (KATEGORİ): Sana verdiğim Kategoriler Listesindeki isimlere bakarak harcamanın türünü mantıklı analiz et. EĞER NE OLDUĞUNU ANLAYAMADIYSAN KESİNLİKLE 106 (DİĞER) KULLAN!
ÖNEMLİ KURAL 6 (ÇÖP VERİLERİ SİL): 'Maxipuan', 'Chip-Para', 'Bonus' gibi ödül kazanımlarını ve tutarı 1 TL'nin altında olan saçma satırları KESİNLİKLE JSON'A EKLEME.
ÖNEMLİ KURAL 7 (TAKSİT KONTROLÜ): Ekstreden okuduğun bir işlem (Örn: Trendyol 2/3) sistemde zaten var olabilir. Senin görevin ekstredeki TÜM harcamaları (ara taksitler dahil) listeye hazırlamaktır. Mükerrer kontrolünü (Kural 4) yaparak her şeyi JSON'a dök.{categoryList}

JSON Formatı Örneği:
[
  {{
    ""Amount"": 395.00,
    ""Description"": ""VELIEFENDI C.I.T.Y. TURI"",
    ""Date"": ""2026-02-28T12:00:00Z"",
    ""InstallmentCount"": 1,
    ""CategoryId"": 4
  }},
  {{
    ""Amount"": 1500.00,
    ""Description"": ""MEDIAMARKT (1/6 Taksit)"",
    ""Date"": ""2026-02-27T14:30:00Z"",
    ""InstallmentCount"": 6,
    ""CategoryId"": 106
  }}
]";

            string combinedText = prompt + "\n\nKullanıcı Ekstresi/Notu:\n" + (string.IsNullOrWhiteSpace(text) ? "Sadece dosya ektedir." : text);
            parts.Add(new { text = combinedText });

            if (fileBytes != null && fileBytes.Length > 0)
            {
                string mimeType = fileExtension?.ToLower() switch
                {
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    _ => "application/pdf"
                };

                parts.Add(new
                {
                    inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(fileBytes) }
                });
            }

            var requestBody = new { contents = new[] { new { parts = parts.ToArray() } } };
            var jsonRequest = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{GeminiEndpoint}?key={_apiKey}", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                if ((int)response.StatusCode == 429)
                {
                    throw new Exception("Gemini API kotası doldu! Yaklaşık 30 saniye sonra tekrar deneyiniz.");
                }
                throw new Exception($"Gemini HTTP Hatası: {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            dynamic? result = JsonConvert.DeserializeObject(responseJson);
            string? aiText = result?.candidates?[0]?.content?.parts?[0]?.text;

            if (string.IsNullOrWhiteSpace(aiText))
            {
                throw new Exception("Gemini boş yanıt döndü. Belge okunamamış veya güvenlik filtresine takılmış olabilir.");
            }

            int startIndex = aiText.IndexOf('[');
            int endIndex = aiText.LastIndexOf(']');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                string cleanJson = aiText.Substring(startIndex, endIndex - startIndex + 1);

                try
                {
                    var parsedList = JsonConvert.DeserializeObject<List<Expense>>(cleanJson);
                    return parsedList ?? new List<Expense>();
                }
                catch (Exception ex)
                {
                    throw new Exception($"JSON Çevirme Hatası! Gemini Yanıtı: {cleanJson} | Sistem: {ex.Message}");
                }
            }
            else
            {
                throw new Exception($"Gemini JSON listesi vermedi! Gelen Metin: {aiText}");
            }
        }


        // ====================================================================================
        // --- YENİ METOT: ASİSTAN (CHATBOT) SOHBET VE DİNAMİK ANALİZ FONKSİYONU ---
        // ====================================================================================
        public async Task<string> AskAssistantAsync(string actionType, string contextDataJson)
        {
            try
            {
                // Yapay zekanın JSON verisini kaçırmaması için etrafını Markdown formatı ile netleştirdik.
                string basePrompt = $@"Sen bir CFO (Finans Direktörü) ciddiyetinde çalışan bir yapay zeka asistanısın. Görevin aşağıda sana JSON formatında iletilen harcama verilerini inceleyerek kullanıcının istediği spesifik aksiyonu yerine getirmektir.

KURALLAR:
1. Üslubun tamamen resmi, net ve profesyonel olmalıdır (Kesinlikle emoji, argo veya laubali kelimeler kullanma).
2. Cevapların doğrudan sorulan soruya odaklanmalıdır. Laf kalabalığı yapma.
3. Para birimi olarak Türk Lirası (₺) kullan, ondalıkları profesyonelce ayır.
4. EĞER GÖNDERİLEN VERİ BOŞSA VEYA KULLANILAMAZ HALDEYSE sadece bunu belirt, varsayım yapma.

Kullanıcının Talep Ettiği İşlem Tipi: {actionType}

SANA SUNULAN FİNANSAL VERİ (JSON FORMATINDA):
```json
{contextDataJson}";

// Seçilen eyleme göre AI'ı yönlendirecek ekstra ipuçları
            if (actionType == "check_duplicates")
            {
                    basePrompt += "\n\nEk Talimat: Sana verilen JSON verisindeki harcamaları tek tek incele. AYNI GÜN içinde ve KURUŞU KURUŞUNA AYNI TUTARDA gerçekleşmiş harcamaları tespit et. Eğer mükerrer (çift çekim) şüphesi varsa, tarihi, tutarı ve açıklamasıyla birlikte net bir şekilde listele ve kullanıcıyı uyar. Eğer her şey normal görünüyorsa 'Mükerrer veya şüpheli işlem bulunmamaktadır.' şeklinde rapor ver.";
                }
            else if (actionType == "give_advice")
                {
                    basePrompt += "\n\nEk Talimat: Verileri incele, en çok harcama yapılan kalemleri ve gereksiz görünen harcamaları tespit et. Daha iyi bir bütçe yönetimi için gerçekçi, şirkette rapor sunar gibi 2 veya 3 maddelik eyleme geçirilebilir finansal tavsiye ver.";
                }
                else if (actionType == "highest_expense")
                {
                    basePrompt += "\n\nEk Talimat: Sana gönderilen veri içerisindeki en yüksek tutarlı harcamayı bul ve detaylarını (Açıklama, Tarih, Tutar) raporla. Kesinlikle verideki en büyük rakamı bulmalısın.";
                }
                else
                {
                    basePrompt += "\n\nEk Talimat: Veriyi genel olarak değerlendir ve finansal durumu özetleyen profesyonel bir sonuç çıkar.";
                }

                var requestBody = new
                {
                    contents = new[]
                    {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = basePrompt }
                        }
                    }
                }
                };

                var jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{GeminiEndpoint}?key={_apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    dynamic? result = JsonConvert.DeserializeObject(responseJson);

                    string? aiText = result?.candidates?[0]?.content?.parts?[0]?.text;
                    return aiText ?? "Yapay zeka asistanı şu an bir yanıt üretemedi. Lütfen daha sonra tekrar deneyiniz.";
                }

                var errorBody = await response.Content.ReadAsStringAsync();
                return $"Sistem Hatası: Asistana şu an erişilemiyor. Detay: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"Beklenmeyen Hata: {ex.Message}";
            }
        }
    }
}