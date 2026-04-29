using System.Collections.Generic;

namespace VeriFinans.Dtos
{
    public class CategoryChainDto
    {
        public List<string> Names { get; set; } = new List<string>();
        public int Type { get; set; } // 0: Gelir, 1: Gider
    }
}