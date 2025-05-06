using Celeste.Mod.ConsistencyTracker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class ChapterMetaInfo {
        public string ChapterDebugName { get; set; }
        public string CampaignName { get; set; }
        public string ChapterName { get; set; }
        public string ChapterSID { get; set; }
        public string ChapterSIDDialogSanitized { get; set; }
        public string MapBin { get; set; }
        public string SideName { get; set; }
        public GoldenTier Tier { get; set; } = new GoldenTier(-1); //-1 = Undetermined
        public int GoldenPoints { get; set; } = -1; //-1 means not set and should be automatically determined throught the tier

        public string SanitizeRoomName(string name) {
            name = name.Replace(";", "");
            return name;
        }

        public string GetChapterNameClean(AreaData area) {
            string chapName = area.Name;
            return chapName.DialogCleanOrNull() ?? chapName.SpacedPascalCase();
        }
        public string GetCampaignNameClean(AreaData area) {
            return DialogExt.CleanLevelSet(area.GetLevelSet());
        }
        public string GetChapterDebugName(Session session) {
            return $"{session.MapData.Data.SID}_{session.Area.Mode}".Replace("/", "_");
        }

        public ChapterMetaInfo(Session session) {
            AreaData area = AreaData.Areas[session.Area.ID];
            string chapNameClean = GetChapterNameClean(area);
            string campaignName = GetCampaignNameClean(area);

            string chapterDebugName = GetChapterDebugName(session);

            ChapterDebugName = chapterDebugName;
            CampaignName = campaignName;
            ChapterName = chapNameClean;
            ChapterSID = session.MapData.Data.SID;
            ChapterSIDDialogSanitized = ConsistencyTrackerModule.SanitizeSIDForDialog(session.MapData.Data.SID);
            MapBin = session.MapData.Filename;
            SideName = session.Area.Mode.ToReadableString();
        }

        public override string ToString() {
            //output all fields in this format: "field1: value1, field2: value2, field3: value3"
            return string.Join(", ", GetType().GetProperties().Select(p => $"{p.Name}: {p.GetValue(this)}"));
        }
    }
}
