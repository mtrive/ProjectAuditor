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
        const string k_EventTopicName = "projectAuditorUsage";

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
            ShowMuted,
            OnlyCriticalIssues,
        };

        // -------------------------------------------------------------------------------------------------------------

        [Serializable]
        struct ProjectAuditorUIButtonEvent
        {
            // camelCase since these events get serialized to Json and naming convention in analytics is camelCase
            public string action;    // Name of the buttom
            public Int64 t_since_start; // Time since app start (in microseconds)
            public Int64 duration; // Duration of event in ticks - 100-nanosecond intervals.
            public Int64 ts; //Timestamp (milliseconds epoch) when action started.

            public ProjectAuditorUIButtonEvent(string name, Analytic analytic)
            {
                action = name;
                t_since_start = SecondsToMicroseconds(analytic.GetStartTime());
                duration = SecondsToTicks(analytic.GetDurationInSeconds());
                ts = analytic.GetTimestamp();
            }
        }


        [Serializable]
        struct ProjectAuditorUIButtonEventWithKeyValues
        {
            [Serializable]
            public struct EventKeyValue
            {
                public string key;
                public string value;
            }

            public string action;
            public Int64 t_since_start;
            public Int64 duration;
            public Int64 ts;
            public EventKeyValue[] action_params;

            public ProjectAuditorUIButtonEventWithKeyValues(string name, Analytic analytic, Dictionary<string, string> payload)
            {
                action = name;
                t_since_start = SecondsToMicroseconds(analytic.GetStartTime());
                duration = SecondsToTicks(analytic.GetDurationInSeconds());
                ts = analytic.GetTimestamp();

                // Convert dictionary to a serializable array of key/value pairs
                if (payload != null && payload.Count > 0)
                {
                    action_params = new EventKeyValue[payload.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, string> kvp in payload)
                    {
                        action_params[i].key = kvp.Key;
                        action_params[i].value = kvp.Value;
                        ++i;
                    }
                }
                else
                {
                    action_params = null;
                }
            }
        }

        [Serializable]
        public struct SelectionSummary
        {
            public int id;
            // stephenm TODO : populate this. But is this currently-visible issues? All of them? Only the un-muted ones?
            //public int totalIssueCount;
            public int selectedCount;
            public int selectedCriticalCount;
        }

        [Serializable]
        struct ProjectAuditorUIButtonEventWithSelectionSummary
        {
            public string action;
            public Int64 t_since_start;
            public Int64 duration;
            public Int64 ts;
            public SelectionSummary[] action_params;

            public ProjectAuditorUIButtonEventWithSelectionSummary(string name, Analytic analytic, SelectionSummary[] payload)
            {
                action = name;
                t_since_start = SecondsToMicroseconds(analytic.GetStartTime());
                duration = SecondsToTicks(analytic.GetDurationInSeconds());
                ts = analytic.GetTimestamp();
                action_params = payload;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        static string GetButtonName(UIButton uiButton)
        {
            switch (uiButton)
            {
                case UIButton.Analyze:
                    return"analyze_button_click";
                case UIButton.Export:
                    return"export_button_click";
                case UIButton.ApiCalls:
                    return"api_tab";
                case UIButton.ProjectSettings:
                    return"settings_tab";
                case UIButton.AssemblySelect:
                    return"assembly_button_click";
                case UIButton.AssemblySelectApply:
                    return"assembly_apply";
                case UIButton.AreaSelect:
                    return"area_button_click";
                case UIButton.AreaSelectApply:
                    return"area_apply";
                case UIButton.Mute:
                    return"mute_button_click";
                case UIButton.Unmute:
                    return"unmute_button_click";
                case UIButton.ShowMuted:
                    return"show_muted_checkbox";
                case UIButton.OnlyCriticalIssues:
                    return"only_hotpath_checkbox";
                default:
                    Debug.LogFormat("SendUIButtonEvent: Unsupported button type : {0}", uiButton);
                    return "";
            }
        }

        static Int64 SecondsToMilliseconds(float seconds)
        {
            return (Int64)(seconds * 1000);
        }

        static Int64 SecondsToTicks(float durationInSeconds)
        {
            return (Int64)(durationInSeconds * 10000);
        }

        static Int64 SecondsToMicroseconds(double seconds)
        {
            return (Int64)(seconds * 1000000);
        }

        static public bool SendUIButtonEvent(UIButton uiButton, Analytic analytic)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
#if UNITY_2018_1_OR_NEWER
                ProjectAuditorUIButtonEvent uiButtonEvent =
                    new ProjectAuditorUIButtonEvent(GetButtonName(uiButton), analytic);

                AnalyticsResult result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiButtonEvent);
                return (result == AnalyticsResult.Ok);
#endif
            }
            return false;
        }

        static public bool SendUIButtonEventWithKeyValues(UIButton uiButton, Analytic analytic, Dictionary<string, string> payload)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
#if UNITY_2018_1_OR_NEWER
                ProjectAuditorUIButtonEventWithKeyValues uiButtonEvent =
                    new ProjectAuditorUIButtonEventWithKeyValues(GetButtonName(uiButton), analytic, payload);

                AnalyticsResult result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiButtonEvent);
                return (result == AnalyticsResult.Ok);
#endif
            }
            return false;
        }

        static public bool SendUIButtonEventWithSelectionSummary(UIButton uiButton, Analytic analytic, SelectionSummary[] payload)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
#if UNITY_2018_1_OR_NEWER
                ProjectAuditorUIButtonEventWithSelectionSummary uiButtonEvent =
                    new ProjectAuditorUIButtonEventWithSelectionSummary(GetButtonName(uiButton), analytic, payload);

                AnalyticsResult result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiButtonEvent);
                return (result == AnalyticsResult.Ok);
#endif
            }
            return false;
        }

        // -------------------------------------------------------------------------------------------------------------
         public class Analytic
         {
             private double m_StartTime;
             private float m_DurationInSeconds;
             private Int64 m_Timestamp;
             private bool m_Blocking;

             public Analytic()
             {
                 m_StartTime = EditorApplication.timeSinceStartup;
                 m_DurationInSeconds = 0;
                 m_Timestamp = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                 m_Blocking = true;
             }

             public void End()
             {
                 m_DurationInSeconds = (float)(EditorApplication.timeSinceStartup - m_StartTime);
             }

             public double GetStartTime()
             {
                 return m_StartTime;
             }

             public float GetDurationInSeconds()
             {
                 return m_DurationInSeconds;
             }

             public Int64 GetTimestamp()
             {
                 return m_Timestamp;
             }

             public bool GetBlocking()
             {
                 return m_Blocking;
             }
         }

         static public Analytic BeginAnalytic()
         {
             return new Analytic();
         }
    }
 }
