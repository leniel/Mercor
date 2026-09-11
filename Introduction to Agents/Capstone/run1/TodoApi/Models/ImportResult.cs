using System.Collections.Generic;

namespace TodoApi.Models
{
    public class ImportResult
    {
        public int TotalProcessed { get; set; }
        public int Imported { get; set; }
        public int Failed { get; set; }
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
    }

    public class ImportError
    {
        public int Row { get; set; }
        public string Error { get; set; }
    }
}

