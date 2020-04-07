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

        // camelCase since these events get serialized to Json and naming convention in analytics is camelCase
        [Serializable]
        struct ProjectAuditorUIButtonEvent
        {
            [Serializable]
            public struct EventKeyValue
            {
                public string key;
                public string value;
            }

            public string action;    // Name of the buttom
            public bool blocking;    // Was this action blocking? (True for all events in Project Auditor, at least until background analysis is implemented)

            //public Dictionary<string, string> action_params; // Custom data for specific event payloads
            public EventKeyValue[] action_params;

            public Int64 t_since_start; // Time since app start (in microseconds)
            public Int64 duration; // Duration of event in ticks - 100-nanosecond intervals.
            public Int64 ts; //Timestamp (milliseconds epoch) when action started.

            public ProjectAuditorUIButtonEvent(string name, Analytic analytic, Dictionary<string, string> payload)
            {
                action = name;
                blocking = analytic.GetBlocking();

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

                t_since_start = SecondsToMicroseconds(analytic.GetStartTime());
                duration = SecondsToTicks(analytic.GetDurationInSeconds());
                ts = analytic.GetTimestamp();
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
        }

        // -------------------------------------------------------------------------------------------------------------

        static public bool SendUIButtonEvent(UIButton uiButton, Analytic analytic, Dictionary<string, string> payload = null)
        {
            analytic.End();

            if (!s_EnableAnalytics)
                return false;

#if UNITY_2018_1_OR_NEWER
            // Duration is in "ticks" 100 nanosecond intervals. I.e. 0.1 microseconds
            //float durationInTicks = SecondsToTicks(durationInSeconds);
            string buttonName = "";
            switch (uiButton)
            {
                case UIButton.Analyze:
                    buttonName = "analyze_button_click";
                    break;
                case UIButton.Export:
                    buttonName = "export_button_click";
                    break;
                case UIButton.ApiCalls:
                    buttonName = "api_tab";
                    break;
                case UIButton.ProjectSettings:
                    buttonName = "settings_tab";
                    break;
                case UIButton.AssemblySelect:
                    buttonName = "assembly_button_click";
                    break;
                case UIButton.AssemblySelectApply:
                    buttonName = "assembly_apply";
                    break;
                case UIButton.AreaSelect:
                    buttonName = "area_button_click";
                    break;
                case UIButton.AreaSelectApply:
                    buttonName = "area_apply";
                    break;
                case UIButton.Mute:
                    buttonName = "mute_button_click";
                    break;
                case UIButton.Unmute:
                    buttonName = "unmute_button_click";
                    break;
                case UIButton.ShowMuted:
                    buttonName = "show_muted_checkbox";
                    break;
                case UIButton.OnlyCriticalIssues:
                    buttonName = "only_hotpath_checkbox";
                    break;
                default:
                    Debug.LogFormat("SendUIButtonEvent: Unsupported button type : {0}", uiButton);
                    return false;
            }

            ProjectAuditorUIButtonEvent uiButtonEvent = new ProjectAuditorUIButtonEvent(buttonName, analytic, payload);

            AnalyticsResult result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiButtonEvent);
            if (result != AnalyticsResult.Ok)
                return false;

            return true;
#else
            return false;
#endif
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
