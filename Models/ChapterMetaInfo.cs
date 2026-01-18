using Celeste.Mod.ConsistencyTracker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class ChapterMetaInfo {
        public string ChapterUID { get; set; }
        public string CampaignName { get; set; }
        public string ChapterName { get; set; }
        public string ChapterSID { get; set; }
        public string ChapterSIDDialogSanitized { get; set; }
        public string MapBin { get; set; }
        public string SideName { get; set; }

        public static string GetChapterNameClean(AreaData area) {
            string chapName = area.Name;
            return chapName.DialogCleanOrNull() ?? chapName.SpacedPascalCase();
        }
        public static string GetCampaignNameClean(AreaData area) {
            return Dialog.CleanLevelSet(area.LevelSet);
        }
        public static string GetChapterUID(Session session) {
            return GetChapterUID(session.MapData.Data.SID, session.Area.Mode);
        }

        public static string GetChapterUID(string sid, AreaMode mode) {
            return $"{sid}/{mode}";
        }

        public static string GetChapterUIDForPath(string uid) {
            return uid.Replace("/", "_");
        }

        public static string GetChapterUIDForPath(string sid, AreaMode mode) {
            return GetChapterUIDForPath(GetChapterUID(sid, mode));
        }

        public ChapterMetaInfo(Session session) {
            AreaData area = AreaData.Areas[session.Area.ID];
            string chapNameClean = GetChapterNameClean(area);
            string campaignName = GetCampaignNameClean(area);

            string chapterDebugName = GetChapterUID(session);

            ChapterUID = chapterDebugName;
            CampaignName = campaignName;
            ChapterName = chapNameClean;
            ChapterSID = session.MapData.Data.SID;
            ChapterSIDDialogSanitized = ConsistencyTrackerModule.SanitizeSidForDialog(session.MapData.Data.SID);
            MapBin = session.MapData.Filename;
            SideName = session.Area.Mode.ToReadableString();
        }

        public override string ToString() {
            //output all fields in this format: "field1: value1, field2: value2, field3: value3"
            return string.Join(", ", GetType().GetProperties().Select(p => $"{p.Name}: {p.GetValue(this)}"));
        }
    }
}
