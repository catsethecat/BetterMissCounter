﻿using IPA;
using IPALogger = IPA.Logging.Logger;
using CountersPlus.Counters.Interfaces;
using TMPro;
using BS_Utils.Gameplay;
using Zenject;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Threading;
using IPA.Config.Stores;
using System.Web;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace BetterMissCounter
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        [Init]
        public Plugin(IPALogger logger, IPA.Config.Config config)
        {
            Instance = this;
            Log = logger;
            TestConfig.Instance = config.Generated<TestConfig>();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Plugin.Log.Info("meow");

        }

        [OnExit]
        public void OnApplicationQuit()
        {

        }
    }

    class TestConfig
    {
        public static TestConfig Instance { get; set; }
        public virtual string TopText { get; set; } = "Misses";
        public virtual Color TopColor { get; set; } = Color.white;
        public virtual string BottomText { get; set; } = "PB: ";
        public virtual Color BottomColor { get; set; } = Color.white;
        public virtual Color LessColor { get; set; } = Color.white;
        public virtual Color EqualColor { get; set; } = Color.yellow;
        public virtual Color MoreColor { get; set; } = Color.red;
        public virtual bool UseScoreSaber { get; set; } = true;
        public virtual bool UseBeatLeader { get; set; } = true;
    }

    class TestUIHost
    {

        public string TopText { get => TestConfig.Instance.TopText; set => TestConfig.Instance.TopText = value; }
        public Color TopColor { get => TestConfig.Instance.TopColor; set => TestConfig.Instance.TopColor = value; }
        public string BottomText { get => TestConfig.Instance.BottomText; set => TestConfig.Instance.BottomText = value; }
        public Color BottomColor { get => TestConfig.Instance.BottomColor; set => TestConfig.Instance.BottomColor = value; }
        public Color LessColor { get => TestConfig.Instance.LessColor; set => TestConfig.Instance.LessColor = value; }
        public Color EqualColor { get => TestConfig.Instance.EqualColor; set => TestConfig.Instance.EqualColor = value; }
        public Color MoreColor { get => TestConfig.Instance.MoreColor; set => TestConfig.Instance.MoreColor = value; }
        public bool UseScoreSaber { get => TestConfig.Instance.UseScoreSaber; set => TestConfig.Instance.UseScoreSaber = value; }
        public bool UseBeatLeader { get => TestConfig.Instance.UseBeatLeader; set => TestConfig.Instance.UseBeatLeader = value; }
    }

    public class CustomCounter : CountersPlus.Counters.Custom.BasicCustomCounter, INoteEventHandler
    {
        TMP_Text missText;
        TMP_Text bottomText;
        int missCount = 0;
        int PBMissCount = -1;

        [Inject] private GameplayCoreSceneSetupData data;

        int difficultyRank;
        string difficulty;
        string levelHash;
        string characteristic;
        string userID;
        string userName;

        public override void CounterInit()
        {

            TMP_Text topText = CanvasUtility.CreateTextFromSettings(Settings);
            missText = CanvasUtility.CreateTextFromSettings(Settings, new Vector3(0, -0.35f, 0));
            bottomText = CanvasUtility.CreateTextFromSettings(Settings, new Vector3(0, -0.65f, 0));

            topText.fontSize = 3f;
            topText.text = TestConfig.Instance.TopText;
            topText.color = TestConfig.Instance.TopColor;
            missText.fontSize = 4f;
            missText.text = "0";
            missText.color = TestConfig.Instance.LessColor;
            bottomText.fontSize = 2f;
            bottomText.color = TestConfig.Instance.BottomColor;

            IDifficultyBeatmap beatmap = data.difficultyBeatmap;

            if (beatmap.level.levelID.IndexOf("custom_level_") != -1) {
                difficultyRank = beatmap.difficultyRank;
                difficulty = beatmap.difficulty.SerializedName();
                characteristic = beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
                levelHash = beatmap.level.levelID.Substring(13);
                userID = GetUserInfo.GetUserID();
                userName = GetUserInfo.GetUserName();
                if (TestConfig.Instance.UseScoreSaber)
                {
                    Thread t = new Thread(new ThreadStart(ScoreSaberThread));
                    t.Start();
                }
                if (TestConfig.Instance.UseBeatLeader)
                {
                    Thread t = new Thread(new ThreadStart(BeatLeaderThread));
                    t.Start();
                }
            }

        }

        static string[] GetStringsBetweenStrings(string str, string start, string end)
        {
            List<string> list = new List<string>();
            for (int found = str.IndexOf(start); found > 0; found = str.IndexOf(start, found + 1))
            {
                int startIndex = found + start.Length;
                int endIndex = str.IndexOf(end, startIndex);
                endIndex = endIndex != -1 ? endIndex : str.IndexOf("\n", startIndex);
                list.Add(str.Substring(startIndex, endIndex - startIndex));
            }
            return list.ToArray();
        }

        public void ScoreSaberThread()
        {
            WebClient client = new WebClient();
            for (int page = 1; ; page++)
            {
                try
                {
                    string res = client.DownloadString("https://scoresaber.com/api/leaderboard/by-hash/" + levelHash + "/scores?page=" + page + "&difficulty=" + difficultyRank + "&gameMode=Solo" + characteristic + "&search=" + HttpUtility.UrlEncode(userName));

                    String[] ids = GetStringsBetweenStrings(res, "\"id\": \"", "\"");
                    String[] missedNotes = GetStringsBetweenStrings(res, "\"missedNotes\": ", ",");
                    String[] badCuts = GetStringsBetweenStrings(res, "\"badCuts\": ", ",");

                    String[] totalItems = GetStringsBetweenStrings(res, "\"total\": ", ",");
                    String[] itemsPerPage = GetStringsBetweenStrings(res, "\"itemsPerPage\": ", ",");

                    for (int i = 0; i < ids.Length; i++)
                    {
                        if(ids[i] == userID)
                        {
                            int totalMisses = Int32.Parse(missedNotes[i]) + Int32.Parse(badCuts[i]);
                            if (PBMissCount == -1 || totalMisses < PBMissCount)
                            {
                                PBMissCount = totalMisses;
                                bottomText.text = TestConfig.Instance.BottomText + PBMissCount;
                            }
                            return;
                        }
                    }

                    if (page == ((Int32.Parse(totalItems[0]) - 1) / Int32.Parse(itemsPerPage[0]) + 1))
                        return;
                }
                catch
                {
                    return;
                }
            }
        }

        public void BeatLeaderThread()
        {
            WebClient client = new WebClient();
            try
            {
                string res = client.DownloadString("https://api.beatleader.xyz/score/" + userID + "/" + levelHash + "/" + difficulty + "/" + characteristic);
                String[] missedNotes = GetStringsBetweenStrings(res, "\"missedNotes\":", ",");
                String[] badCuts = GetStringsBetweenStrings(res, "\"badCuts\":", ",");
                if(missedNotes.Length > 0)
                {
                    int totalMisses = Int32.Parse(missedNotes[0]) + Int32.Parse(badCuts[0]);
                    if (PBMissCount == -1 || totalMisses < PBMissCount)
                    {
                        PBMissCount = totalMisses;
                        bottomText.text = TestConfig.Instance.BottomText + PBMissCount;
                    }
                    return;
                }
            }
            catch
            {
                return;
            }
        }

        public override void CounterDestroy()
        {

        }

        public void OnNoteCut(NoteData data, NoteCutInfo info)
        {
            if (!info.allIsOK && data.colorType != ColorType.None) UpdateCount(1);
        }

        public void OnNoteMiss(NoteData data)
        {
            if (data.colorType != ColorType.None) UpdateCount(1);
        }

        public void UpdateCount(int add = 0)
        {
            missCount += add;
            missText.text = ""+missCount;
            if(PBMissCount > -1)
            {
                missText.color = missCount < PBMissCount ? TestConfig.Instance.LessColor :
                    missCount == PBMissCount ? TestConfig.Instance.EqualColor : TestConfig.Instance.MoreColor;
            }
        }

    }
}
