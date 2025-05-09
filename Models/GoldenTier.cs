using System;
using System.Collections.Generic;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class GoldenTier {
        
        //A full tier is centered around the 0.5 mark
        //So Tier 5.0 is very low in Tier 5, Tier 5.5 is the center, and Tier 5.99 is very high in Tier 5
        
        public int Sort { get; set; }
        public double Fraction { get; set; }
        public double FullSort => Sort + Fraction;

        public GoldenTier(int sort) {
            Sort = sort;
            Fraction = 0.5f;
        }
        public GoldenTier(int sort, double fraction) {
            Sort = sort;
            Fraction = fraction;
        }

        public string GetTierString(bool withFrac = false) {
            if (Sort == -1) return "Undetermined";
            else if (Sort == 0) return "Untiered";
            if (withFrac) {
                string paddedFraction = Fraction.ToString("0.00").Substring(1);
                if (Fraction >= 0.99f) paddedFraction = ".99"; //Edge case: rounding puts it at 1.00, substring would make it .00
                else if (Math.Abs(Fraction - 0.5f) < 0.00001f) paddedFraction = ""; //Special case: Dont show fraction if its exactly the center
                return $"Tier {Sort}{paddedFraction}";
            }
            return $"Tier {Sort}";
        }

        public override string ToString() {
            return GetTierString();
        }

        public double GetGp(double baseValue = 1.43f) {
            if (Sort <= 0) return 0;
            return Math.Pow(baseValue, FullSort - 1.5f);
        }

        public static GoldenTier GetTierByGp(double gp, double baseValue = 1.43f) {
            if (gp < 1) {
                return new GoldenTier(-1);
            }
            //Do the inverse calculation of above to arrive at a FullSort value
            double fullSort = Math.Log(gp, baseValue) + 1.5f;
            int sort = (int)fullSort;
            double fraction = fullSort - sort;
            return new GoldenTier(sort, fraction);
        }
        
        public static List<GoldenTier> GetTiers() {
            List<GoldenTier> tiers = new List<GoldenTier>();
            for (int i = -1; i <= 19; i++) {
                tiers.Add(new GoldenTier(i));
            }
            return tiers;
        }

        public override bool Equals(object obj) {
            return obj != null && 
                   obj is GoldenTier other && 
                   Sort == other.Sort;
        }
    }
}