using System;
static class VideoFullscreenBridge {
 public const string Marker=" [PaneLock video]";
 public static string CleanTitle(string title){return title.Replace(Marker,"");}
 public static void Observe(Engine engine,Entry e,string title){bool active=e.Chrome&&title.IndexOf(Marker,StringComparison.Ordinal)>=0;if(active==e.VideoActive)return;e.VideoActive=active;
  if(active){e.VideoOwnsCrop=false;e.VideoPending=!e.PaneFullscreen;e.VideoDeadline=DateTime.UtcNow.AddSeconds(4);}
  else{e.VideoPending=false;if(e.VideoOwnsCrop){engine.RestoreCrop(e);e.ReenterAt=DateTime.MinValue;}e.VideoOwnsCrop=false;}
 }
 public static void Enter(Engine engine,Entry e){if(!e.VideoPending||!e.VideoActive||engine.Paused)return;e.VideoPending=false;if(engine.ToggleChromeFullscreen(e.H)){e.VideoOwnsCrop=true;return;}if(DateTime.UtcNow<e.VideoDeadline)e.VideoPending=true;}
}
