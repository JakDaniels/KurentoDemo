using Kurento.NET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KurentoDemo.Hubs
{
    public class RoomSession
    {
        public string RoomID { set; get; }
        public MediaPipeline Pipeline { set; get; }
        public ConcurrentDictionary<string, UserSession> UserSessions { set; get; }
        public async Task RemoveAsync(string id)
        {
            if (UserSessions.TryRemove(id, out UserSession user))
            {
                // Release own resources
                if (user.SendEndPoint != null)
                {
                    await user.SendEndPoint.ReleaseAsync();
                }
                if (user.ReceviedEndPoints != null)
                {
                    foreach (var endPoint in user.ReceviedEndPoints.Values)
                    {
                        await endPoint.ReleaseAsync();
                    }
                }
                // Release resources of other users
                foreach (var u in UserSessions.Values)
                {
                    if (u.ReceviedEndPoints.TryRemove(id, out WebRtcEndpoint endpoint))
                    {
                        await endpoint.ReleaseAsync();
                    }
                }
            }
        }
        public IEnumerable<UserSession> GetOtherUsers(string connectionId) => UserSessions.Values.Where(x => x.Id != connectionId);
    }
}
