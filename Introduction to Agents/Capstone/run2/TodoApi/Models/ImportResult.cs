using System.Collections.Generic;

namespace TodoApi.Models
{
    /// <summary>
    /// Result of a CSV import operation, including counts and per-row errors.
    /// </summary>
    public class ImportResult
    {
        /// <summary>Total number of data rows processed (excluding the header).</summary>
        public int TotalProcessed { get; set; }

        /// <summary>Number of rows successfully imported.</summary>
        public int Imported { get; set; }

        /// <summary>Number of rows rejected due to validation errors.</summary>
        public int Failed { get; set; }

        /// <summary>Details for each rejected row.</summary>
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
    }

    /// <summary>
    /// Describes a single rejected CSV row.
    /// </summary>
    public class ImportError
    {
        /// <summary>1-indexed row number in the CSV file (header = row 1).</summary>
        public int Row { get; set; }

        /// <summary>Human-readable reason the row was rejected.</summary>
        public string Error { get; set; }
    }
}
