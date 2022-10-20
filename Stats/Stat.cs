using Celeste.Mod.ConsistencyTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Stats {
    public abstract class Stat {

        public List<string> Identificators;

        public Stat(List<string> pIdentificators) {
            Identificators = pIdentificators;
        }

        public virtual bool ContainsIdentificator(string format) {
            return Identificators.Exists((id) => format.Contains(id));
        }

        /// <summary>Formats the data for this stat.</summary>
        /// <param name="chapterPath">The chapter path.</param>
        /// <param name="chapterStats">The chapter stats.</param>
        /// <param name="format">The format.</param>
        /// <returns>The format with the appropriate data inserted</returns>
        public abstract string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format);

        /// <summary>Formats the data for the summary export.</summary>
        /// <param name="chapterPath">The chapter path.</param>
        /// <param name="chapterStats">The chapter stats.</param>
        /// <returns>A string to which comprises a section in the summary export
        /// for this stat, or null when this stat shouldn't be added to the summary</returns>
        public abstract string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats);


        public virtual List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return null;
        }
        public virtual List<StatFormat> GetStatExamples() {
            return null;
        }
        
    }
}
