﻿using System.Collections.Generic;
using Toolbox.Collections;
using Toolbox.Math;
using UnityEngine;
using UnityEngine.SceneManagement;
using Toolbox.Messaging;

namespace Toolbox.Behaviours
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
    
    
    /// <summary>
    /// Base class for creating components that can track objects.
    /// </summary>
    public abstract class AbstractTracker : MonoBehaviour
    {
        [Tooltip("Can a single target be added to the tracking list more than once? Usually, should be left false.")]
        public bool AllowRepeats = false;
        [Tooltip("The names of Targeted spawns that can be tracked by this follower. Any targeted spawn using this name will be added/removed to this followers list as they spawn/despawn.")]
        public string[] AllowedIds;

        List<int> HashedIds;
        public List<float> Weights { get; private set; }
        public List<Transform> Trans { get; private set; }
        public Transform MyTrans { get; private set; }

        public bool HasTargets
        {
            get { return Trans.Count > 0; }
        }

        public Vector3 Centroid
        {
            get { return Trans.Count > 0 ? MathUtils.GetCentroid(Trans) : MyTrans.position; }
        }

        public Vector3 WeightedCentroid
        {
            get { return Trans.Count > 0 ? MathUtils.GetCentroid(Trans, Weights) : MyTrans.position; }
        }

        public Vector3 WeightLimitedCentroid
        {
            get { return Trans.Count > 0 ? MathUtils.GetLimitedCentroid(Trans, Weights) : MyTrans.position; }
        }

        /// <summary>
        /// Returns a list of transform for all trackable objects with the given id.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public List<Trackable> GetAllOfId(string id)
        {
            HashedString hash = new HashedString(id);
            List<Trackable> trans = new List<Trackable>();
            foreach (var t in Trans)
            {
                var track = t.GetComponent<Trackable>();
                if (track.Id.Hash == hash.Hash)
                    trans.Add(track);
            }

            return trans;
        }

        /// <summary>
        /// Stops tracking any objects with the given id.
        /// </summary>
        /// <param name="id"></param>
        public void StopTracking(string id)
        {
            var tracks = GetAllOfId(id);
            foreach (var t in tracks)
            {
                int index = Trans.IndexOf(t.transform);
                if (index >= 0)
                {
                    Trans.RemoveAt(index);
                    Weights.RemoveAt(index);
                    OnEndTracking(new TrackableRemovedEvent(t));
                }
            }
        }

        /// <summary>
        /// Returns a list of Vector3s that composites positions of all transforms and bodies.
        /// WARNING: This list is volitile and temporary and should not be cached!
        /// </summary>
        public List<Vector3> Positions
        {
            get
            {
                var TempVec3s = SharedArrayFactory.RequestTempList<Vector3>();
                for (int i = 0; i < Trans.Count; i++)
                    TempVec3s.Add(Trans[i].position);
                return TempVec3s;
            }
        }


        protected virtual void Awake()
        {
            Trans = new List<Transform>();
            Weights = new List<float>();

            HashedIds = new List<int>(AllowedIds.Length);
            for (int i = 0; i < AllowedIds.Length; i++)
                HashedIds.Add(HashedString.StringToHash(AllowedIds[i]));
            GlobalMessagePump.Instance.AddListener<TrackableSpawnedEvent>(HandleSpawned);
            GlobalMessagePump.Instance.AddListener<TrackableRemovedEvent>(HandleRemoved);

            MyTrans = transform;
        }

        protected virtual void OnDestroy()
        {
            GlobalMessagePump.Instance.RemoveListener<TrackableSpawnedEvent>(HandleSpawned);
            GlobalMessagePump.Instance.RemoveListener<TrackableRemovedEvent>(HandleRemoved);
        }

        void HandleSpawned(TrackableSpawnedEvent msg)
        {
            if (HashedIds.Contains(msg.Target.Id.Hash))
            {
                if (TypeHelper.IsReferenceNull(msg.Target))
                {
                    //this is probably an old reference from last scene - remove it and move on
                    Trans.Remove(null);
                }
                else if (AllowRepeats || !Trans.Contains(msg.Target.transform))
                {
                    Trans.Add(msg.Target.transform);
                    Weights.Add(msg.Target.Weight);
                    OnBeginTracking(msg);
                }
            }

        }

        void HandleRemoved(TrackableRemovedEvent msg)
        {
            int index = Trans.IndexOf(msg.Target.transform);
            if (index >= 0)
            {
                Trans.RemoveAt(index);
                Weights.RemoveAt(index);
                OnEndTracking(msg);
            }
        }

        protected abstract void OnBeginTracking(TrackableSpawnedEvent msg);
        protected abstract void OnEndTracking(TrackableRemovedEvent msg);
    }
}