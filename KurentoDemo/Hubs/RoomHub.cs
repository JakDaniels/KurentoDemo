﻿using System;
using System.Threading.Tasks;
using Kurento.NET;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Concurrent;

namespace KurentoDemo.Hubs
{
    public class RoomHub : DynamicHub
    {
        private readonly RoomSessionManager _roomManager;

        private readonly KurentoClient _kurento;
        public RoomHub(RoomSessionManager roomSessionManager, KurentoClient kurento)
        {
            _roomManager = roomSessionManager;
            _kurento = kurento;
       
        }
        public string UserName
        {
            get
            {
                return Context.GetHttpContext().Request.Query["userName"];
            }
        }

        public string RoomID
        {
            get
            {
                return Context.GetHttpContext().Request.Query["roomId"];
            }
        }

        public override async Task OnConnectedAsync()
        {

            var roomSession = await _roomManager.GetRoomSessionAsync(RoomID);
            var userSession = new UserSession()
            {
                Id = Context.ConnectionId,
                ReceivedEndPoints = new ConcurrentDictionary<string, WebRtcEndpoint>(),
                SendEndPoint = null,
                UserName = UserName

            };
            roomSession.UserSessions.TryAdd(Context.ConnectionId, userSession);
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomID);
            Clients.Caller.SetLocalUser(userSession);
            Clients.Caller.SetOtherUsers(roomSession.GetOtherUsers(Context.ConnectionId));
            Clients.OthersInGroup(RoomID).OtherJoined(userSession);
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var roomSession = await _roomManager.GetRoomSessionAsync(RoomID);
            await roomSession.RemoveAsync(Context.ConnectionId);
            Clients.OthersInGroup(RoomID).OtherLeft(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        private async Task<WebRtcEndpoint> GetEndPointAsync(string id)
        {
            var roomSession = await _roomManager.GetRoomSessionAsync(RoomID);
            if (roomSession.UserSessions.TryGetValue(Context.ConnectionId, out UserSession selfSession))
            {
                if (Context.ConnectionId == id)
                {
                    if (selfSession.SendEndPoint == null)
                    {
                        selfSession.SendEndPoint = await _kurento.CreateAsync(new WebRtcEndpoint(roomSession.Pipeline));
                        selfSession.SendEndPoint.OnIceCandidate += arg =>
                        {
                            Clients.Caller.AddCandidate(id, arg.candidate);
                        };
                    }
                    return selfSession.SendEndPoint;
                }
                else
                {
                    if (roomSession.UserSessions.TryGetValue(id, out UserSession otherSession))
                    {
                        if (otherSession.SendEndPoint == null)
                        {
                            otherSession.SendEndPoint = await _kurento.CreateAsync(new WebRtcEndpoint(roomSession.Pipeline));
                            otherSession.SendEndPoint.OnIceCandidate += arg =>
                            {
                                Clients.Client(id).AddCandidate(id, arg.candidate);
                            };
                        }
                        if (!selfSession.ReceivedEndPoints.TryGetValue(id, out WebRtcEndpoint otherEndPoint))
                        {
                            otherEndPoint = await _kurento.CreateAsync(new WebRtcEndpoint(roomSession.Pipeline));
                            otherEndPoint.OnIceCandidate += arg =>
                            {
                                Clients.Caller.AddCandidate(id, arg.candidate);
                            };
                            await otherSession.SendEndPoint.ConnectAsync(otherEndPoint);
                            selfSession.ReceivedEndPoints.TryAdd(id, otherEndPoint);
                        }
                        return otherEndPoint;
                    }
                }
            }
            return default(WebRtcEndpoint);
        }
        public async Task ProcessCandidateAsync(string id, IceCandidate candidate)
        {
            var endPoint = await GetEndPointAsync(id);
            await endPoint.AddIceCandidateAsync(candidate);
        }
        public async Task ProcessOfferAsync(string id, string offerSDP)
        {
            var endPoint = await GetEndPointAsync(id);
            var answerSDP = await endPoint.ProcessOfferAsync(offerSDP);
            Clients.Caller.ProcessAnswer(id, answerSDP);
            await endPoint.GatherCandidatesAsync();
        }
    }
}
