using System.Collections.Generic;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class GoldenTier {
        public int Sort { get; set; }
        
        public GoldenTier(int sort) {
            Sort = sort;
        }

        public override string ToString() {
            if (Sort == -1) return "Undetermined";
            else if (Sort == 0) return "Untiered";
            return $"Tier {Sort}";
        }
        
        public static List<GoldenTier> GetTiers() {
            List<GoldenTier> tiers = new List<GoldenTier>();
            for (int i = -1; i <= 19; i++) {
                tiers.Add(new GoldenTier(i));
            }
            return tiers;
        }
    }
}