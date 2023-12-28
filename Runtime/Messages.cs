
using Peg.MessageDispatcher;

namespace Peg.Trackables
{
    /// <summary>
    /// This message is posted by objects that, upon spawning, may be tracked by listeners of this message.
    /// A good example would be a target that a camera wants to know it should follow.
    /// </summary>
    public class TrackableSpawnedEvent : TargetMessage<Trackable, TrackableSpawnedEvent>, IDeferredMessage, IBufferedMessage
    {
        public TrackableSpawnedEvent(Trackable target) : base(target) { }
    }


    /// <summary>
    /// Posted when a previously trackable object should no longer be tracked by listeners.
    /// </summary>
    public class TrackableRemovedEvent : TargetMessage<Trackable, TrackableRemovedEvent>
    {
        public TrackableRemovedEvent(Trackable target) : base(target) { }
    }
}
