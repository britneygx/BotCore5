﻿/*

Copyright © 2019 Tara Piccari (Aria; Tashia Redrose)
Licensed under the GPLv2

*/


using System;
using System.Collections.Generic;
using Bot;
using Bot.CommandSystem;
using Bot.NonCommands;
using Newtonsoft.Json;
using OpenMetaverse;

namespace Bot.CommandSystem
{
    public class CommandManager
    {
        public CommandRegistry register;
        public GridClient cl;
        public Logger Log = BotSession.Instance.Logger;
        public string newReply;
        public unsafe bool RunChatCommand(string cmdData)
        {
            register = CommandRegistry.Instance;
            Dictionary<UUID, int> BotAdmins = MainConfiguration.Instance.BotAdmins;
            dynamic parameters = JsonConvert.DeserializeObject(cmdData);
            string request = parameters.request;
            if (parameters.source == "sys") return false;
            string[] para = request.Split(new[] { ' ' });
            Dictionary<string, string> dstuf = new Dictionary<string, string>();
            UUID fromID = UUID.Zero;
            try
            {
                string ID = parameters.from;
                fromID = UUID.Parse(ID);
            }
            catch (Exception e)
            {
                Log.info(log:e.Message);
            }
            int userLevel = 0;
            if (BotAdmins.ContainsKey(fromID) && BotAdmins.Count > 0)
                userLevel = BotAdmins[fromID];
            else if (BotAdmins.Count == 0) userLevel = 5;

            UUID agentKey = UUID.Zero;
            UUID sessID = UUID.Zero;
            string sess = parameters.from_sess;
            if (sess != "")
            {
                sessID = UUID.Parse(sess);
            }


            cl = BotSession.Instance.grid;

            Destinations sourceLoc = new Destinations();
            if (parameters.type == "chat") sourceLoc = Destinations.DEST_LOCAL;
            else if (parameters.type == "group") sourceLoc = Destinations.DEST_GROUP;
            else if (parameters.type == "im") sourceLoc = Destinations.DEST_AGENT;
            else if (parameters.type == "discord") sourceLoc = Destinations.DEST_DISCORD;
            else sourceLoc = Destinations.DEST_LOCAL;

            string agentName = parameters.fromName;
            if(sourceLoc == Destinations.DEST_LOCAL)
            {
                GroupLog.Instance.WriteLogEntry(true, false, agentName, agentKey, request);
            }else if (sourceLoc == Destinations.DEST_AGENT)
            {
                GroupLog.Instance.WriteLogEntry(false, true, agentName, agentKey, request);
            }


            if (sourceLoc == Destinations.DEST_GROUP)
            {
                agentKey = fromID;
                fromID = sessID;

                // Initiate group log saver
                string GroupName = cl.Groups.GroupName2KeyCache[fromID];

                GroupLog.Instance.WriteLogEntry(GroupName, "secondlife:///app/agent/" + agentKey.ToString() + "/about (" + agentName + ") : " + request);


                if (agentKey == cl.Self.AgentID) return false;
            }
            else {
                agentKey = fromID;

                if (agentKey == cl.Self.AgentID) return false;
            }

            if (request.Substring(0, 1) != "!")
            {
                // Check if active bug or feature report session. If not- return.
                nRegistry.Dispatch(request, agentKey, ((parameters.source == "obj") ? "Object" : agentName), sourceLoc, fromID);
                /*
                if (ocb.ActiveReportSessions.ContainsKey(agentKey) && ocb.ActiveReportSessions.Count > 0)
                {
                    // Send report response to GitCommands
                    GitCommands gc = new GitCommands();
                    gc.BugResponse(fromID, agentKey, ocb.ActiveReportSessions[agentKey].ReportStage, request, sourceLoc, MHE, agentName);
                    return false;
                }

                if (ocb.ActiveFeatureSessions.ContainsKey(agentKey) && ocb.ActiveFeatureSessions.Count > 0)
                {
                    GitCommands gc = new GitCommands();
                    gc.FeatureResponse(fromID, agentKey, ocb.ActiveFeatureSessions[agentKey].ReportStage, request, sourceLoc, MHE, agentName);
                    return false;
                }

                if (ocb.ActiveCommentSessions.ContainsKey(agentKey) && ocb.ActiveCommentSessions.Count > 0)
                {
                    GitCommands gc = new GitCommands();
                    gc.comment(fromID, agentKey, ocb.ActiveCommentSessions[agentKey].ReportStage, request, sourceLoc, MHE, agentName);
                    return false;
                }

                if (ocb.NoticeSessions.ContainsKey(agentKey) && ocb.NoticeSessions.Count > 0)
                {
                    GroupSystem gs = new GroupSystem();
                    gs.update_notice_sess(fromID, agentKey, request, sourceLoc, MHE, agentName);
                    return false;
                }

                if (ocb.MailingLists.Count > 0)
                {
                    // Scan all mailing lists for a session and agentKey that match.
                    foreach (string sML in ocb.MailingLists.Keys)
                    {
                        OCBotMemory.MailList ML = ocb.MailingLists[sML];
                        if (ML.PrepFrom == agentKey && ML.PrepState == 1)
                        {
                            MailingLists.MailingLists cML = new MailingLists.MailingLists();
                            cML.HandleMailListData(agentKey, fromID, sourceLoc, MHE, sML, request);
                            return false;
                        }
                    }
                }*/
                return false;
            }
            else
            {
                request = request.Substring(1);
                para = request.Split(' ');
            }
            try
            {

                register.RunCommand(request, fromID, userLevel, sourceLoc, agentKey, agentName);
            }
            catch (Exception e)
            {
                string Msg = e.Message;
                Msg = Msg.Replace("ZNI", "");
                int i;
                int* ptr = &i;
                IntPtr addr = (IntPtr)ptr;
                MessageFactory.Post(Destinations.DEST_LOCAL, "Exception caught: [" + Msg + "]\n \n[STACK] " + e.StackTrace.Replace("ZNI", "") + "\nMemory Position: 0x" + addr.ToString("x") + "\nCommand: " + request + $"\nMisc Details: {fromID}, {userLevel}, {sourceLoc}, {agentKey}, {agentName}", UUID.Zero);
                
                // do nothing here. 
            }
            Log.info(log:"Leaving command parser");
            return false;
        }

        public CommandManager()
        {
            Log = BotSession.Instance.Logger;
        }
    }
}
