using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace M8.VR.EventSystems {
    [AddComponentMenu("M8/VR Event/Graphic Raycaster")]
    [RequireComponent(typeof(Canvas))]
    public class GraphicRaycaster : BaseRaycaster {
        protected const int kNoEventMaskSet = -1;

        public override int sortOrderPriority {
            get {
                // We need to return the sorting order here as distance will all be 0 for overlay.
                if(canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvas.sortingOrder;

                return base.sortOrderPriority;
            }
        }

        public override int renderOrderPriority {
            get {
                // We need to return the sorting order here as distance will all be 0 for overlay.
                if(canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvas.renderOrder;

                return base.renderOrderPriority;
            }
        }

        public override Camera eventCamera {
            get {
                return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }
        }

        [SerializeField]
        private bool _ignoreReversedGraphics = true;
        [SerializeField]
        private bool _isBlockObjects = false;

        public bool ignoreReversedGraphics { get { return _ignoreReversedGraphics; } set { _ignoreReversedGraphics = value; } }
        public bool isBlocking3DObjects { get { return _isBlockObjects; } set { _isBlockObjects = value; } }

        [SerializeField]
        protected LayerMask _BlockingMask = kNoEventMaskSet;

        [Tooltip("How far to check for collision, use -1 for infinite distance.")]
        [SerializeField]
        protected float _distance = -1f;

        protected Vector3[] mCanvasWorldCorners = new Vector3[4]; //BottomLeft, TopLeft, TopRight, BottomRight

        private Canvas mCanvas;

        private Canvas canvas {
            get {
                if(mCanvas != null)
                    return mCanvas;

                mCanvas = GetComponent<Canvas>();
                return mCanvas;
            }
        }

        private List<Graphic> mRaycastResults = new List<Graphic>();

        protected GraphicRaycaster() { }
                
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) {
            //Need Controller
            if(!ControllerManager.isInstantiated)
                return;

            var ctrl = ControllerManager.instance.GetActiveByDeviceID(eventData.pointerId);
            if(!ctrl)
                return;

            if(canvas == null)
                return;
            
            //Requires canvas to be in WorldSpace
            if(canvas.renderMode != RenderMode.WorldSpace) {
                return;
            }

            //Requires event camera
            if(eventCamera == null)
                return;

            //Grab center of Canvas
            //convert 3d position to rect world space (via canvas (need origin))
            var rectT = canvas.GetComponent<RectTransform>();
            rectT.GetWorldCorners(mCanvasWorldCorners);

            //grab center based on pivot
            var quadRight = Vector3.Lerp(mCanvasWorldCorners[0], mCanvasWorldCorners[3], rectT.pivot.x);
            var quadUp = Vector3.Lerp(mCanvasWorldCorners[0], mCanvasWorldCorners[1], rectT.pivot.y);
            var center = quadUp + quadRight;

            Vector3 up = rectT.up, right = rectT.right;


            //Grab position within canvas
            var rayCtrl = new Ray(ctrl.transform.position, ctrl.transform.forward);
            var canvasPlane = new Plane(-rectT.forward, center);
            float rayCtrlDist;

            //no collision?
            if(!canvasPlane.Raycast(rayCtrl, out rayCtrlDist))
                return;

            //too far?
            if(_distance > 0f && _distance < rayCtrlDist)
                return;

            var eventPosition3D = rayCtrl.GetPoint(rayCtrlDist);
            var eventPosition = eventCamera.WorldToScreenPoint(eventPosition3D);
            
            float hitDistance = float.MaxValue;

            Ray ray = new Ray();

            if(eventCamera != null)
                ray = eventCamera.ScreenPointToRay(eventPosition);

            if(isBlocking3DObjects) {
                float dist = 100.0f;

                if(eventCamera != null)
                    dist = eventCamera.farClipPlane - eventCamera.nearClipPlane;

                RaycastHit hit;
                if(Physics.Raycast(ray, out hit, dist, _BlockingMask))
                    hitDistance = hit.distance;

                //2D colliders?
            }

            mRaycastResults.Clear();
            Raycast(canvas, eventCamera, eventPosition, mRaycastResults);

            for(var index = 0; index < mRaycastResults.Count; index++) {
                var go = mRaycastResults[index].gameObject;
                bool appendGraphic = true;

                if(ignoreReversedGraphics) {
                    if(eventCamera == null) {
                        // If we dont have a camera we know that we should always be facing forward
                        var dir = go.transform.rotation * Vector3.forward;
                        appendGraphic = Vector3.Dot(Vector3.forward, dir) > 0;
                    }
                    else {
                        // If we have a camera compare the direction against the cameras forward.
                        var cameraFoward = eventCamera.transform.rotation * Vector3.forward;
                        var dir = go.transform.rotation * Vector3.forward;
                        appendGraphic = Vector3.Dot(cameraFoward, dir) > 0;
                    }
                }

                if(appendGraphic) {
                    float distance = 0;

                    if(eventCamera == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        distance = 0;
                    else {
                        Transform trans = go.transform;
                        Vector3 transForward = trans.forward;
                        // http://geomalgorithms.com/a06-_intersect-2.html
                        distance = (Vector3.Dot(transForward, trans.position - ray.origin) / Vector3.Dot(transForward, ray.direction));

                        // Check to see if the go is behind the camera.
                        if(distance < 0)
                            continue;
                    }

                    if(distance >= hitDistance)
                        continue;

                    var castResult = new RaycastResult {
                        gameObject = go,
                        module = this,
                        distance = distance,
                        screenPosition = eventPosition,
                        worldPosition = eventPosition3D,
                        worldNormal = canvasPlane.normal,
                        index = resultAppendList.Count,
                        depth = mRaycastResults[index].depth,
                        sortingLayer = canvas.sortingLayerID,
                        sortingOrder = canvas.sortingOrder
                    };
                    resultAppendList.Add(castResult);
                }
            }
        }
                
        /// <summary>
        /// Perform a raycast into the screen and collect all graphics underneath it.
        /// </summary>
        [NonSerialized] static readonly List<Graphic> s_SortedGraphics = new List<Graphic>();
        private static void Raycast(Canvas canvas, Camera eventCamera, Vector2 pointerPosition, List<Graphic> results) {
            // Debug.Log("ttt" + pointerPoision + ":::" + camera);
            // Necessary for the event system
            var foundGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            for(int i = 0; i < foundGraphics.Count; ++i) {
                Graphic graphic = foundGraphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if(graphic.depth == -1 || !graphic.raycastTarget)
                    continue;

                if(!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera))
                    continue;

                if(graphic.Raycast(pointerPosition, eventCamera)) {
                    s_SortedGraphics.Add(graphic);
                }
            }

            s_SortedGraphics.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));
            //		StringBuilder cast = new StringBuilder();
            for(int i = 0; i < s_SortedGraphics.Count; ++i)
                results.Add(s_SortedGraphics[i]);
            //		Debug.Log (cast.ToString());

            s_SortedGraphics.Clear();
        }
    }
}