using Peg.Lib;
using UnityEngine;

namespace Peg.Behaviours
{
    /// <summary>
    /// Follows the geometric center of multiple targets on a configurable number of axies.
    /// Can automatically listen for TargetedSpawn events and add them to the list of targets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SmoothFollowTrackables : AbstractTracker
    {
        [Tooltip("How fast to follow on the x-axis.")]
        public float X = 0.3f;
        [Tooltip("How fast to follow on the y-axis.")]
        public float Y = 0.3f;
        [Tooltip("How fast to follow on the z-axis.")]
        public float Z = 0.3f;
        [Tooltip("Can be configured to give smoother results in certain situations.")]
        public UpdateTiming Mode;
        [Tooltip("The dead-zone for each axis when tracking motion.")]
        public Vector3 DeadZone;
        [Compact]
        [Tooltip("An offset from the current targets' centroid.")]
        public Vector3 Offset;
        [Tooltip("Should this tracker ignore trackable weights?")]
        public bool IgnoreWeights = true;
        [Tooltip("Only used if 'IgnoreWeights' is false. Determines if a limted weighting should be used.")]
        public bool LimitedWeight = false;
        [Tooltip("Beyond this distance, this follower will simply snap to the location is is tracking.")]
        public float SnapLimit = 1000000;

        [HideInInspector]
        public float DeadZoneStop = 0.5f;
        [Tooltip("Should we ignore attached rigidbody for position and only use transform positions?")]
        public bool IgnoreRigidbody = true;


        /// <summary>
        /// The last position this tracker calculated itself to be at. The next update will
        /// blend from this position towards the newly determined position.
        /// </summary>
        public Vector3 BlendFrom { get { return Last; } set { Last = value; } }
        

        Vector3 Last;
        Rigidbody MyBody;


        //bool xDead;

        //bool yDead;
        //bool zDead;

        protected override void Awake()
        {
            base.Awake();
            MyBody = GetComponent<Rigidbody>();
            Last = transform.position;
        }

        void Step(float timeDelta)
        {
            if (Time.timeScale == 0) return;

            Vector3 curPos;
            if (HasTargets)
            { 
                curPos = MyTrans.position;
                Vector3 targetPos;
                if (IgnoreWeights)
                    targetPos = Centroid + Offset;
                else
                {
                    if (LimitedWeight)
                        targetPos = WeightLimitedCentroid + Offset;
                    else targetPos = WeightedCentroid + Offset;
                }

                var dist = Vector3.Distance(curPos, targetPos);
                if (dist > SnapLimit)
                    curPos = targetPos;
                else
                {
                    float x, y, z;
                    if (X == 0) x = Last.x + Offset.x;
                    else
                    {
                        float tx = targetPos.x - (DeadZone.x * -Mathf.Sign(curPos.x - targetPos.x));
                        x = Mathf.Abs(curPos.x - targetPos.x) < DeadZone.x ? curPos.x : MathUtils.SmoothApproach(curPos.x, Last.x, tx, X, timeDelta);
                        /*
                        if (xDead)
                        {
                            //deadzone was exceeded on x-axis, try to move back to center
                            if (Mathf.Abs(targetPos.x - curPos.x) < DeadZoneStop)
                                xDead = false;
                            x = MathUtils.SmoothApproach(curPos.x, Last.x, targetPos.x, X, timeDelta);
                        }
                        else
                        {
                            if (Mathf.Abs(targetPos.x - curPos.x) > DeadZone.x)
                                xDead = true;
                            x = MathUtils.SmoothApproach(curPos.x, Last.x, curPos.x, X, timeDelta);
                        }
                        */
                    }
                    if (Y == 0) y = Last.y + Offset.y;
                    else y = Mathf.Abs(curPos.y - targetPos.y) < DeadZone.y ? curPos.y : MathUtils.SmoothApproach(curPos.y, Last.y, targetPos.y, Y, timeDelta);

                    if (Z == 0) z = Last.z + Offset.z;
                    else z = Mathf.Abs(curPos.z - targetPos.z) < DeadZone.z ? curPos.z : MathUtils.SmoothApproach(curPos.z, Last.z, targetPos.z, Z, timeDelta);

                    curPos = new Vector3(x, y, z);
                }
            }
            else curPos = Centroid;

            //NOTE: Technically, this should now take our deadzone into consideration and be set to
            //targetPos for each dead-zoned axis - but higher smoothing helps fix it too
            Last = curPos;


            if (!IgnoreRigidbody && MyBody != null) MyBody.position = curPos;
            else MyTrans.position = curPos;
        }

        void Update()
        {
            if (Mode == UpdateTiming.Update)
                Step(Time.unscaledDeltaTime);
        }

        void LateUpdate()
        {
            if (Mode == UpdateTiming.LateUpdate)
                Step(Time.unscaledDeltaTime);
        }

        void FixedUpdate()
        {
            if (Mode == UpdateTiming.FixedUpdate)
                Step(Time.deltaTime);
        }

        protected override void OnBeginTracking(TrackableSpawnedEvent msg) { }

        protected override void OnEndTracking(TrackableRemovedEvent msg) { }
    }
}
