using UnityEngine;
using UnityEngine.SceneManagement;
using Peg.Messaging;
using Peg.MessageDispatcher;

namespace Peg.Trackables
{
    /// <summary>
    /// Attach this to any object that should potentially be tracked upon spawning.
    /// Listeners of this message can make use of such 'trackables' any way they see fit.
    /// A good example would be a camera that wants to know when an important object, such
    /// as a player, has spawned or despawned so that it knows to start or stop following
    /// that object.
    /// </summary>
    public class Trackable : AbstractAuthoritativeGlobalPost<TrackableSpawnedEvent>
    {
        public HashedString Id;
        public NotifyWhen OccursAt = NotifyWhen.Enable;
        [Tooltip("The weight that is applied by a tracker when considering this trackable.")]
        public float Weight = 1;

        bool PostedOnEnable;
        bool Posted;
        public enum NotifyWhen
        {
            Start,
            Enable,
        }

        protected virtual void Start()
        {
            if (OccursAt == NotifyWhen.Start)
                Init();
            SceneManager.sceneUnloaded += SceneUnloaded;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneUnloaded -= SceneUnloaded;
            base.OnDestroy();
            if (Posted)
                GlobalMessagePump.Instance.PostMessage(new TrackableRemovedEvent(this));
        }

        protected virtual void OnEnable()
        {
            if (Id.Value == null) Id.Value = string.Empty;
            if (OccursAt == NotifyWhen.Enable)
            {
                PostedOnEnable = true;
                Init();
            }
        }

        protected virtual void OnDisable()
        {
            if (Posted && PostedOnEnable)
                GlobalMessagePump.Instance.PostMessage(new TrackableRemovedEvent(this));
            Posted = false;
        }

        protected override TrackableSpawnedEvent ActivateMsg()
        {
            return new TrackableSpawnedEvent(this);
        }

        void Init()
        {
            if (ValidateAuthority())
            {
                PostMessage();
                Posted = true;
            }
        }

        void SceneUnloaded(Scene scene)
        {
            CleanupMessage();
        }
    }
    
    
}