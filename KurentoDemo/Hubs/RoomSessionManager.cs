﻿using Kurento.NET;
using Microsoft.AspNetCore.Hosting;
using KurentoDemo.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KurentoDemo.Hubs
{
    public class RoomSessionManager
    {
        private readonly ConcurrentDictionary<string, RoomSession> _roomSessions;
        private readonly KurentoClient _client;
 

        public RoomSessionManager(KurentoClient client)
        {
            _client = client;
            _roomSessions = new ConcurrentDictionary<string, RoomSession>();
        }
        public async Task<RoomSession> GetRoomSessionAsync(string roomID)
        {
            if (!_roomSessions.TryGetValue(roomID, out RoomSession roomSession))
            {
                var pipeline = await _client.CreateAsync(new MediaPipeline());
                roomSession = new RoomSession()
                {
                    RoomID = roomID,
                    Pipeline = pipeline,
                    UserSessions = new ConcurrentDictionary<string, UserSession>()
                };
                _roomSessions.TryAdd(roomID, roomSession);
            }
            return roomSession;
        }

        /// <summary>
        /// Release room resources if there are no users
        /// </summary>
        /// <param name="roomID"></param>
        /// <returns></returns>
        public async Task TryRemoveRoomAsync(string roomID)
        {
            // Release room resources if there are no users
            if (_roomSessions.TryGetValue(roomID, out RoomSession roomSession))
            {
                if (roomSession.UserSessions.IsEmpty)
                {
                    await roomSession.Pipeline.ReleaseAsync();
                    _roomSessions.TryRemove(roomID, out _);
                }
            }
        }
    }
}
