namespace GorillaLocomotion
{
    using UnityEngine;

    public class Player : MonoBehaviour
    {
        private static Player _instance;
        public static Player Instance { get { return _instance; } }

        [Header("Original from lemming\nmodified by Keo.CS to make it simple")]

        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;
        public Transform leftHandFollower;
        public Transform rightHandFollower;
        public Transform rightHandTransform;
        public Transform leftHandTransform;

        private Vector3 lastLeftPos;
        private Vector3 lastRightPos;
        private Vector3 lastHeadPos;
        private Rigidbody rb;

        public int velocityHistorySize = 5;
        public float maxArmLength = 1.5f;
        public float unStickDistance = 1f;
        public float velocityLimit = 1f;
        public float maxJumpSpeed = 8f;
        public float jumpMultiplier = 1.1f;
        public float rayRadius = 0.05f;
        public float slideFactor = 0.03f;
        public float precision = 0.995f;

        private Vector3[] velocityHistory;
        private int velocityIndex;
        private Vector3 currentVelocity;
        private Vector3 avgVelocity;
        private Vector3 lastPosition;

        public Vector3 rightHandOffset;
        public Vector3 leftHandOffset;
        public LayerMask locomotionEnabledLayers;
        public bool wasLeftHandTouching;
        public bool wasRightHandTouching;
        public bool disableMovement = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            
            rb = GetComponent<Rigidbody>();
            velocityHistory = new Vector3[velocityHistorySize];
            lastLeftPos = leftHandFollower.transform.position;
            lastRightPos = rightHandFollower.transform.position;
            lastHeadPos = headCollider.transform.position;
            lastPosition = transform.position;
        }

        private Vector3 GetLeftHandPos()
        {
            Vector3 pos = leftHandTransform.position + leftHandTransform.rotation * leftHandOffset;
            Vector3 toHand = pos - headCollider.transform.position;
            if (toHand.magnitude > maxArmLength)
                pos = headCollider.transform.position + toHand.normalized * maxArmLength;
            return pos;
        }

        private Vector3 GetRightHandPos()
        {
            Vector3 pos = rightHandTransform.position + rightHandTransform.rotation * rightHandOffset;
            Vector3 toHand = pos - headCollider.transform.position;
            if (toHand.magnitude > maxArmLength)
                pos = headCollider.transform.position + toHand.normalized * maxArmLength;
            return pos;
        }

        private void Update()
        {
            bodyCollider.transform.eulerAngles = new Vector3(0, headCollider.transform.eulerAngles.y, 0);

            bool leftTouching = false;
            bool rightTouching = false;
            Vector3 leftMove = Vector3.zero;
            Vector3 rightMove = Vector3.zero;

            Vector3 leftTravel = GetLeftHandPos() - lastLeftPos + Vector3.down * 19.6f * Time.deltaTime * Time.deltaTime;
            Vector3 finalPos;
            
            if (CheckCollision(lastLeftPos, rayRadius, leftTravel, out finalPos))
            {
                leftMove = wasLeftHandTouching ? lastLeftPos - GetLeftHandPos() : finalPos - GetLeftHandPos();
                rb.velocity = Vector3.zero;
                leftTouching = true;
            }

            Vector3 rightTravel = GetRightHandPos() - lastRightPos + Vector3.down * 19.6f * Time.deltaTime * Time.deltaTime;
            
            if (CheckCollision(lastRightPos, rayRadius, rightTravel, out finalPos))
            {
                rightMove = wasRightHandTouching ? lastRightPos - GetRightHandPos() : finalPos - GetRightHandPos();
                rb.velocity = Vector3.zero;
                rightTouching = true;
            }

            Vector3 totalMove = Vector3.zero;
            if ((leftTouching || wasLeftHandTouching) && (rightTouching || wasRightHandTouching))
                totalMove = (leftMove + rightMove) / 2;
            else
                totalMove = leftMove + rightMove;

            Vector3 headMove = headCollider.transform.position + totalMove - lastHeadPos;
            if (CheckCollision(lastHeadPos, headCollider.radius, headMove, out finalPos))
            {
                totalMove = finalPos - lastHeadPos;
                RaycastHit hit;
                if (Physics.Raycast(lastHeadPos, headMove, out hit, headMove.magnitude + headCollider.radius * precision * 0.999f, locomotionEnabledLayers.value))
                    totalMove = lastHeadPos - headCollider.transform.position;
            }

            if (totalMove != Vector3.zero)
                transform.position += totalMove;

            lastHeadPos = headCollider.transform.position;

            Vector3 leftFinalTravel = GetLeftHandPos() - lastLeftPos;
            if (CheckCollision(lastLeftPos, rayRadius, leftFinalTravel, out finalPos))
            {
                lastLeftPos = finalPos;
                leftTouching = true;
            }
            else
            {
                lastLeftPos = GetLeftHandPos();
            }

            Vector3 rightFinalTravel = GetRightHandPos() - lastRightPos;
            if (CheckCollision(lastRightPos, rayRadius, rightFinalTravel, out finalPos))
            {
                lastRightPos = finalPos;
                rightTouching = true;
            }
            else
            {
                lastRightPos = GetRightHandPos();
            }

            UpdateVelocity();

            if ((rightTouching || leftTouching) && !disableMovement)
            {
                if (avgVelocity.magnitude > velocityLimit)
                {
                    float jumpSpeed = avgVelocity.magnitude * jumpMultiplier;
                    if (jumpSpeed > maxJumpSpeed) jumpSpeed = maxJumpSpeed;
                    rb.velocity = avgVelocity.normalized * jumpSpeed;
                }
            }

            RaycastHit hitInfo;
            if (leftTouching && (GetLeftHandPos() - lastLeftPos).magnitude > unStickDistance && 
                !Physics.SphereCast(headCollider.transform.position, rayRadius * precision, GetLeftHandPos() - headCollider.transform.position, out hitInfo, (GetLeftHandPos() - headCollider.transform.position).magnitude - rayRadius, locomotionEnabledLayers.value))
            {
                lastLeftPos = GetLeftHandPos();
                leftTouching = false;
            }

            if (rightTouching && (GetRightHandPos() - lastRightPos).magnitude > unStickDistance && 
                !Physics.SphereCast(headCollider.transform.position, rayRadius * precision, GetRightHandPos() - headCollider.transform.position, out hitInfo, (GetRightHandPos() - headCollider.transform.position).magnitude - rayRadius, locomotionEnabledLayers.value))
            {
                lastRightPos = GetRightHandPos();
                rightTouching = false;
            }

            leftHandFollower.position = lastLeftPos;
            rightHandFollower.position = lastRightPos;
            wasLeftHandTouching = leftTouching;
            wasRightHandTouching = rightTouching;
        }

        private bool CheckCollision(Vector3 start, float radius, Vector3 movement, out Vector3 endPos)
        {
            RaycastHit hit;
            if (Physics.SphereCast(start, radius * precision, movement, out hit, movement.magnitude + radius * (1 - precision), locomotionEnabledLayers.value))
            {
                endPos = hit.point + hit.normal * radius;
                
                RaycastHit innerHit;
                if (Physics.SphereCast(start, radius * precision * precision, endPos - start, out innerHit, (endPos - start).magnitude + radius * (1 - precision * precision), locomotionEnabledLayers.value))
                {
                    endPos = start + (endPos - start).normalized * Mathf.Max(0, hit.distance - radius * (1f - precision * precision));
                }
                else if (Physics.Raycast(start, endPos - start, out innerHit, (endPos - start).magnitude + radius * precision * precision * 0.999f, locomotionEnabledLayers.value))
                {
                    endPos = start;
                }
                
                Vector3 slideMove = Vector3.ProjectOnPlane(start + movement - endPos, hit.normal) * slideFactor;
                Vector3 slidePos;
                if (Physics.SphereCast(endPos, radius, slideMove, out innerHit, slideMove.magnitude + radius, locomotionEnabledLayers.value))
                {
                    endPos = innerHit.point + innerHit.normal * radius;
                }
                else if (Physics.SphereCast(slideMove + endPos, radius, start + movement - (slideMove + endPos), out innerHit, (start + movement - (slideMove + endPos)).magnitude + radius, locomotionEnabledLayers.value))
                {
                    endPos = innerHit.point + innerHit.normal * radius;
                }
                
                return true;
            }
            else if (Physics.Raycast(start, movement, out hit, movement.magnitude + radius * precision * 0.999f, locomotionEnabledLayers.value))
            {
                endPos = start;
                return true;
            }
            
            endPos = Vector3.zero;
            return false;
        }

        public bool IsHandTouching(bool leftHand)
        {
            return leftHand ? wasLeftHandTouching : wasRightHandTouching;
        }

        public void Turn(float degrees)
        {
            transform.RotateAround(headCollider.transform.position, transform.up, degrees);
            Quaternion rotation = Quaternion.Euler(0, degrees, 0);
            avgVelocity = rotation * avgVelocity;
            for (int i = 0; i < velocityHistory.Length; i++)
                velocityHistory[i] = rotation * velocityHistory[i];
        }

        private void UpdateVelocity()
        {
            velocityIndex = (velocityIndex + 1) % velocityHistorySize;
            Vector3 oldVelocity = velocityHistory[velocityIndex];
            currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
            avgVelocity += (currentVelocity - oldVelocity) / (float)velocityHistorySize;
            velocityHistory[velocityIndex] = currentVelocity;
            lastPosition = transform.position;
        }
    }
}