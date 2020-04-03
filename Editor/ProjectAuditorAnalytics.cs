using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Analytics;
using UnityEngine;
using UnityEngine.Analytics;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditorAnalytics
    {
        const int k_MaxEventsPerHour = 100;
        const int k_MaxEventItems = 1000;
        const string k_VendorKey = "unity.projectauditor";
        const string k_EventTopicName = "usability";

        static bool s_EnableAnalytics = false;

        public static void EnableAnalytics()
        {
#if UNITY_2018_1_OR_NEWER
            AnalyticsResult result = EditorAnalytics.RegisterEventWithLimit(k_EventTopicName, k_MaxEventsPerHour, k_MaxEventItems, k_VendorKey);
            if (result == AnalyticsResult.Ok)
                s_EnableAnalytics = true;
#endif
        }

        public enum UIButton
        {
            Analyze,
            Export,
            ApiCalls,
            ProjectSettings,
            AssemblySelect,
            AssemblySelectApply,
            AreaSelect,
            AreaSelectApply,
            Mute,
            Unmute,
            ShowMuted
        };

        // public enum UIUsageMode
        // {
        //     Single,
        //     Comparison,
        // };

        public enum UIVisibility
        {
            FrameTimeContextMenu,
            Filters,
            TopTen,
            Frames,
            Threads,
            Markers,
        };

        public enum UIResizeView
        {
            Single,
            Comparison,
        };


        [Serializable]
        struct ProjectAuditorUIButtonEventParameters
        {
            public string name;

            public ProjectAuditorUIButtonEventParameters(string name)
            {
                this.name = name;
            }
        }

        // camelCase since these events get serialized to Json and naming convention in analytics is camelCase
        [Serializable]
        struct ProjectAuditorUIButtonEvent
        {
            public ProjectAuditorUIButtonEvent(string name, float durationInTicks)
            {
                subtype = "profileAnalyzerUIButton";

                // ts is auto added so no need to include it here
                //ts = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                this.duration = durationInTicks;

                parameters = new ProjectAuditorUIButtonEventParameters(name);
            }

            public string subtype;
            //public int ts;
            public float duration;  // Duration is in "ticks" 100 nanosecond intervals. I.e. 0.1 microseconds
            public ProjectAuditorUIButtonEventParameters parameters;
        }

        // [Serializable]
        // struct ProjectAuditorUIUsageEventParameters
        // {
        //     public string name;
        //
        //     public ProjectAuditorUIUsageEventParameters(string name)
        //     {
        //         this.name = name;
        //     }
        // }
        //
        // [Serializable]
        // struct ProjectAuditorUIUsageEvent
        // {
        //     public ProjectAuditorUIUsageEvent(string name, float durationInTicks)
        //     {
        //         subtype = "profileAnalyzerModeUsage";
        //
        //         this.duration = durationInTicks;
        //
        //         parameters = new ProjectAuditorUIUsageEventParameters(name);
        //     }
        //
        //     public string subtype;
        //     public float duration;  // Duration is in "ticks" 100 nanosecond intervals. I.e. 0.1 microseconds
        //     public ProjectAuditorUIUsageEventParameters parameters;
        // }

        [Serializable]
        struct ProjectAuditorUIVisibilityEventParameters
        {
            public string name;
            public bool show;

            public ProjectAuditorUIVisibilityEventParameters(string name, bool show)
            {
                this.name = name;
                this.show = show;
            }
        }

        [Serializable]
        struct ProjectAuditorUIVisibilityEvent
        {
            public ProjectAuditorUIVisibilityEvent(string name, float durationInTicks, bool show)
            {
                subtype = "profileAnalyzerUIVisibility";

                this.duration = durationInTicks;

                parameters = new ProjectAuditorUIVisibilityEventParameters(name, show);
            }

            public string subtype;
            public float duration;  // Duration is in "ticks" 100 nanosecond intervals. I.e. 0.1 microseconds
            public ProjectAuditorUIVisibilityEventParameters parameters;
        }

        [Serializable]
        struct ProjectAuditorUIResizeEventParameters
        {
            public string name;
            public float width;
            public float height;
            public float screenWidth;
            public float screenHeight;
            public bool docked;

            public ProjectAuditorUIResizeEventParameters(string name, float width, float height, float screenWidth, float screenHeight, bool isDocked)
            {
                this.name = name;
                this.width = width;
                this.height = height;
                this.screenWidth = screenWidth;
                this.screenHeight = screenHeight;
                docked = isDocked;
            }
        }

        [Serializable]
        struct ProjectAuditorUIResizeEvent
        {
            public ProjectAuditorUIResizeEvent(string name, float durationInTicks, float width, float height, float screenWidth, float screenHeight, bool isDocked)
            {
                subtype = "profileAnalyzerUIResize";

                this.duration = durationInTicks;

                parameters = new ProjectAuditorUIResizeEventParameters(name, width, height, screenWidth, screenHeight, isDocked);
            }

            public string subtype;
            public float duration;  // Duration is in "ticks" 100 nanosecond intervals. I.e. 0.1 microseconds
            public ProjectAuditorUIResizeEventParameters parameters;
        }

        static float SecondsToTicks(float durationInSeconds)
        {
            return durationInSeconds * 10000;
        }

        public static bool SendUIButtonEvent(UIButton uiButton, float durationInSeconds)
        {
            if (!s_EnableAnalytics)
                return false;

#if UNITY_2018_1_OR_NEWER
            // Duration is in "ticks" 100 nanosecond intervals. I.e. 0.1 microseconds
            float durationInTicks = SecondsToTicks(durationInSeconds);

            ProjectAuditorUIButtonEvent uiButtonEvent;
            switch (uiButton)
            {
                case UIButton.Analyze:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorAnalyze", durationInTicks);
                    break;
                case UIButton.Export:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorExport", durationInTicks);
                    break;
                case UIButton.ApiCalls:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorApiCalls", durationInTicks);
                    break;
                case UIButton.ProjectSettings:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorProjectSettings", durationInTicks);
                    break;
                case UIButton.AssemblySelect:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorAssemblySelect", durationInTicks);
                    break;
                case UIButton.AssemblySelectApply:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorAssemblyApply", durationInTicks);
                    break;
                case UIButton.AreaSelect:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorAreaSelect", durationInTicks);
                    break;
                case UIButton.AreaSelectApply:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorAreaApply", durationInTicks);
                    break;
                case UIButton.Mute:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorMute", durationInTicks);
                    break;
                case UIButton.Unmute:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorUnmute", durationInTicks);
                    break;
                case UIButton.ShowMuted:
                    uiButtonEvent = new ProjectAuditorUIButtonEvent("projectAuditorShowMuted", durationInTicks);
                    break;
                default:
                    Debug.LogFormat("SendUIButtonEvent: Unsupported button type : {0}", uiButton);
                    return false;
            }


            AnalyticsResult result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiButtonEvent);
            if (result != AnalyticsResult.Ok)
                return false;

            return true;
#else
            return false;
#endif
        }

//         public static bool SendUIUsageModeEvent(UIUsageMode uiUsageMode, float durationInSeconds)
//         {
//             if (!s_EnableAnalytics)
//                 return false;
//
// #if UNITY_2018_1_OR_NEWER
//             // Duration is in "ticks" 100 nanosecond intervals. I.e. 0.1 microseconds
//             float durationInTicks = SecondsToTicks(durationInSeconds);
//
//             ProjectAuditorUIUsageEvent uiUsageEvent;
//             switch (uiUsageMode)
//             {
//                 case UIUsageMode.Single:
//                     uiUsageEvent = new ProjectAuditorUIUsageEvent("profileAnalyzerSingle", durationInTicks);
//                     break;
//                 case UIUsageMode.Comparison:
//                     uiUsageEvent = new ProjectAuditorUIUsageEvent("profileAnalyzerCompare", durationInTicks);
//                     break;
//                 default:
//                     Debug.LogFormat("SendUsageEvent: Unsupported usage mode : {0}", uiUsageMode);
//                     return false;
//             }
//
//
//             AnalyticsResult result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiUsageEvent);
//             if (result != AnalyticsResult.Ok)
//                 return false;
//
//             return true;
// #else
//             return false;
// #endif
//         }

        public static bool SendUIVisibilityEvent(UIVisibility uiVisibility, float durationInSeconds, bool show)
        {
            if (!s_EnableAnalytics)
                return false;

#if UNITY_2018_1_OR_NEWER
            // Duration is in "ticks" 100 nanosecond intervals. I.e. 0.1 microseconds
            float durationInTicks = SecondsToTicks(durationInSeconds);

            ProjectAuditorUIVisibilityEvent uiUsageEvent;
            switch (uiVisibility)
            {
                case UIVisibility.FrameTimeContextMenu:
                    uiUsageEvent = new ProjectAuditorUIVisibilityEvent("projectAuditorFrameTimeContextMenu", durationInTicks, show);
                    break;
                case UIVisibility.Filters:
                    uiUsageEvent = new ProjectAuditorUIVisibilityEvent("projectAuditorFilters", durationInTicks, show);
                    break;
                case UIVisibility.TopTen:
                    uiUsageEvent = new ProjectAuditorUIVisibilityEvent("projectAuditorTopTen", durationInTicks, show);
                    break;
                case UIVisibility.Frames:
                    uiUsageEvent = new ProjectAuditorUIVisibilityEvent("projectAuditorFrames", durationInTicks, show);
                    break;
                case UIVisibility.Threads:
                    uiUsageEvent = new ProjectAuditorUIVisibilityEvent("projectAuditorThreads", durationInTicks, show);
                    break;
                case UIVisibility.Markers:
                    uiUsageEvent = new ProjectAuditorUIVisibilityEvent("projectAuditorMarkers", durationInTicks, show);
                    break;
                default:
                    Debug.LogFormat("SendUIVisibilityEvent: Unsupported visibililty item : {0}", uiVisibility);
                    return false;
            }

            AnalyticsResult result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiUsageEvent);
            if (result != AnalyticsResult.Ok)
                return false;

            return true;
#else
            return false;
#endif
        }

        //public static bool SendUIResizeEvent(UIResizeView uiResizeView, float durationInSeconds, float width, float height, bool isDocked)
        public static bool SendUIResizeEvent(float durationInSeconds, float width, float height, bool isDocked)
        {
            if (!s_EnableAnalytics)
                return false;

#if UNITY_2018_1_OR_NEWER
            // Duration is in "ticks" 100 nanosecond intervals. I.e. 0.1 microseconds
            float durationInTicks = SecondsToTicks(durationInSeconds);

            ProjectAuditorUIResizeEvent uiResizeEvent =
                new ProjectAuditorUIResizeEvent("profileAnalyzer", durationInTicks, width, height, 
                    Screen.currentResolution.width, Screen.currentResolution.height, isDocked);
            // switch (uiResizeView)
            // {
            //     case UIResizeView.Single:
            //         // Screen.width, Screen.height is game view size
            //         uiResizeEvent = new ProjectAuditorUIResizeEvent("profileAnalyzerSingle", durationInTicks, width, height, Screen.currentResolution.width, Screen.currentResolution.height, isDocked);
            //         break;
            //     case UIResizeView.Comparison:
            //         uiResizeEvent = new ProjectAuditorUIResizeEvent("profileAnalyzerCompare", durationInTicks, width, height, Screen.currentResolution.width, Screen.currentResolution.height, isDocked);
            //         break;
            //     default:
            //         Debug.LogFormat("SendUIResizeEvent: Unsupported view : {0}", uiResizeView);
            //         return false;
            // }

            AnalyticsResult result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiResizeEvent);
            if (result != AnalyticsResult.Ok)
                return false;

            return true;
#else
            return false;
#endif
        }


        public class Analytic
        {
            double m_StartTime;
            float m_DurationInSeconds;

            public Analytic()
            {
                m_StartTime = EditorApplication.timeSinceStartup;
                m_DurationInSeconds = 0;
            }

            public void End()
            {
                m_DurationInSeconds = (float)(EditorApplication.timeSinceStartup - m_StartTime);
            }

            public float GetDurationInSeconds()
            {
                return m_DurationInSeconds;
            }
        }

        static public Analytic BeginAnalytic()
        {
            return new Analytic();
        }

        static public void SendUIButtonEvent(UIButton uiButton, Analytic instance)
        {
            instance.End();
            SendUIButtonEvent(uiButton, instance.GetDurationInSeconds());
        }

        // static public void SendUIUsageModeEvent(UIUsageMode uiUsageMode, Analytic instance)
        // {
        //     instance.End();
        //     SendUIUsageModeEvent(uiUsageMode, instance.GetDurationInSeconds());
        // }
    }
}