using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace M8.VR.EventSystems {
    /// <summary>
    /// Simple event system using physics raycasts. No camera needed. You can put this anywhere, e.g. under InputModule
    /// </summary>
    [AddComponentMenu("M8/VR Event/Physics Raycaster")]
    public class PhysicsRaycaster : BaseRaycaster {
        public LayerMask layerMask;

        [Tooltip("How far to check for collision, use -1 for infinite distance.")]
        public float distance = -1f;

        protected Vector3[] mCanvasWorldCorners = new Vector3[4]; //BottomLeft, TopLeft, TopRight, BottomRight

        public override Camera eventCamera {
            get {
                return Camera.main;
            }
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) {
            if(!ControllerManager.isInstantiated)
                return;
                        
            var ctrl = ControllerManager.instance.GetActiveByDeviceID(eventData.pointerId);
            if(!ctrl)
                return;
            
            var origin = ctrl.transform.position;
            var dir = ctrl.transform.forward;
            var dist = distance < 0f ? Mathf.Infinity : distance;

            RaycastHit hit;
            if(Physics.Raycast(origin, dir, out hit, dist, layerMask)) {
                var go = hit.collider.gameObject;
                var posHit = hit.point;
                                
                Vector2 plainPt;

                //check for Canvas and grab plain position to emulate screenPosition
                var canvasHit = go.GetComponentInParent<Canvas>();
                if(canvasHit) {
                    canvasHit = canvasHit.rootCanvas;

                    //convert 3d position to rect world space (via canvas (need origin))
                    var rectT = canvasHit.GetComponent<RectTransform>();
                    rectT.GetWorldCorners(mCanvasWorldCorners);

                    //grab center based on pivot
                    var quadRight = Vector3.Lerp(mCanvasWorldCorners[0], mCanvasWorldCorners[3], rectT.pivot.x);
                    var quadUp = Vector3.Lerp(mCanvasWorldCorners[0], mCanvasWorldCorners[1], rectT.pivot.y);
                    var center = quadUp + quadRight;

                    Vector3 up = rectT.up, right = rectT.right;

                    Vector3 dpos = posHit - center;

                    plainPt = new Vector2(Vector3.Dot(up, dpos), Vector3.Dot(right, dpos));
                }
                else
                    plainPt = Vector2.zero;
                //

                //grab proper result
                var result = new RaycastResult {
                    gameObject = go,
                    module = null,
                    distance = hit.distance,
                    worldPosition = posHit,
                    worldNormal = hit.normal,
                    screenPosition = plainPt,
                    index = resultAppendList.Count,
                    sortingLayer = 0,
                    sortingOrder = 0
                };

                resultAppendList.Add(result);
            }
        }
    }
}