using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace M8.VR {
    public class InputModule : BaseInputModule {
        public const int hitCastCacheCount = 8;

        [SerializeField]
        bool _forceModuleActive;

        public bool forceModuleActive {
            get { return _forceModuleActive; }
            set { _forceModuleActive = value; }
        }

        public ControlMap leftClick = ControlMap.Trigger;
        public LayerMask layerMask;

        [Tooltip("How far to check for collision, use -1 for infinite distance.")]
        public float distance = -1f;

        public float dragThreshold = 0.1f;

        /// <summary>
        /// key = id, value = PointerEventData
        /// </summary>
        protected Dictionary<int, PointerEventData> mPointerData = new Dictionary<int, PointerEventData>();
        protected Dictionary<int, Canvas> mPointerCanvas = new Dictionary<int, Canvas>();

        protected Vector3[] mCanvasWorldCorners = new Vector3[4]; //BottomLeft, TopLeft, TopRight, BottomRight
                
        protected bool GetPointerData(int id, out PointerEventData data, bool create) {
            if(!mPointerData.TryGetValue(id, out data) && create) {
                data = new PointerEventData(eventSystem) {
                    pointerId = id,
                };
                mPointerData.Add(id, data);
                return true;
            }
            return false;
        }

        protected void RemovePointerData(PointerEventData data) {
            mPointerData.Remove(data.pointerId);

            if(mPointerCanvas.ContainsKey(data.pointerId))
                mPointerCanvas.Remove(data.pointerId);
        }

        protected PointerEventData GetControllerPointerEventData(Controller ctrl, out bool pressed, out bool released) {
            PointerEventData pointerData;
            var created = GetPointerData(ctrl.deviceID, out pointerData, true);

            pointerData.Reset();

            pressed = created || ctrl.GetButtonPressed(leftClick);
            released = ctrl.GetButtonReleased(leftClick);
            
            pointerData.button = PointerEventData.InputButton.Left;

            //
            //ray cast objects
            var origin = ctrl.transform.position;
            var dir = ctrl.transform.forward;
            var dist = distance < 0f ? Mathf.Infinity : distance;

            RaycastHit hit;
            RaycastResult result;

            if(Physics.Raycast(origin, dir, out hit, dist, layerMask)) {
                var go = hit.collider.gameObject;
                var posHit = hit.point;

                //grab proper result
                result = new RaycastResult {
                    gameObject = go,
                    module = null,
                    distance = hit.distance,
                    worldPosition = posHit,
                    worldNormal = hit.normal,
                    //screenPosition = eventData.position,
                    index = m_RaycastResultCache.Count,
                    sortingLayer = 0,
                    sortingOrder = 0
                };

                Canvas canvasHit = null;

                if(ctrl.GetButtonDown(leftClick)) { //grab from previous
                    mPointerCanvas.TryGetValue(ctrl.deviceID, out canvasHit);
                }

                if(!canvasHit) {
                    canvasHit = go.GetComponentInParent<Canvas>();
                    if(canvasHit) {
                        canvasHit = canvasHit.rootCanvas;

                        //cache canvas
                        if(mPointerCanvas.ContainsKey(ctrl.deviceID))
                            mPointerCanvas[ctrl.deviceID] = canvasHit;
                        else
                            mPointerCanvas.Add(ctrl.deviceID, canvasHit);
                    }
                }

                //grab 2D position
                if(canvasHit) {
                    //convert 3d position to rect world space (via canvas (need origin)), apply to pointerData.position and delta
                    var rectT = canvasHit.GetComponent<RectTransform>();
                    rectT.GetWorldCorners(mCanvasWorldCorners);

                    //grab center based on pivot
                    var quadRight = Vector3.Lerp(mCanvasWorldCorners[0], mCanvasWorldCorners[3], rectT.pivot.x);
                    var quadUp = Vector3.Lerp(mCanvasWorldCorners[0], mCanvasWorldCorners[1], rectT.pivot.y);                    
                    var center = quadUp + quadRight;

                    Vector3 up = rectT.up, right = rectT.right;

                    Vector3 dpos = posHit - center;

                    Vector2 plainPt = new Vector2(Vector3.Dot(up, dpos), Vector3.Dot(right, dpos));

                    result.screenPosition = plainPt; //not quite screen position, but what are you gonna do
                    
                    // generate position/delta for pointer
                    if(created)
                        pointerData.position = plainPt;

                    if(pressed)
                        pointerData.delta = Vector2.zero;
                    else
                        pointerData.delta = plainPt - pointerData.position;

                    pointerData.position = plainPt;
                    //
                }
                else {
                    result.screenPosition = pointerData.position;
                    pointerData.delta = Vector2.zero;
                }
            }
            else
                result = new RaycastResult();
            //

            pointerData.pointerCurrentRaycast = result;

            return pointerData;
        }

        protected void ClearSelection() {
            var baseEventData = GetBaseEventData();

            foreach(var pointer in mPointerData.Values) {
                // clear all selection
                HandlePointerExitAndEnter(pointer, null);
            }

            mPointerData.Clear();
            mPointerCanvas.Clear();
            eventSystem.SetSelectedGameObject(null, baseEventData);
        }

        protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent) {
            // Selection tracking
            var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            // if we have clicked something new, deselect the old thing
            // leave 'selection handling' up to the press event though.
            if(selectHandlerGO != eventSystem.currentSelectedGameObject)
                eventSystem.SetSelectedGameObject(null, pointerEvent);
        }

        protected PointerEventData GetLastPointerEventData(int id) {
            PointerEventData data;
            GetPointerData(id, out data, false);
            return data;
        }

        private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold) {
            if(!useDragThreshold)
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }

        protected virtual void ProcessMove(PointerEventData pointerEvent) {
            var targetGO = (Cursor.lockState == CursorLockMode.Locked ? null : pointerEvent.pointerCurrentRaycast.gameObject);
            HandlePointerExitAndEnter(pointerEvent, targetGO);
        }

        protected virtual void ProcessDrag(PointerEventData pointerEvent) {
            if(!pointerEvent.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                pointerEvent.pointerDrag == null)
                return;

            if(!pointerEvent.dragging
                && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position, dragThreshold, pointerEvent.useDragThreshold)) {
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            // Drag notification
            if(pointerEvent.dragging) {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if(pointerEvent.pointerPress != pointerEvent.pointerDrag) {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                    pointerEvent.eligibleForClick = false;
                    pointerEvent.pointerPress = null;
                    pointerEvent.rawPointerPress = null;
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
            }
        }

        public override bool IsPointerOverGameObject(int pointerId) {
            var lastPointer = GetLastPointerEventData(pointerId);
            if(lastPointer != null)
                return lastPointer.pointerEnter != null;
            return false;
        }

        protected void CopyFromTo(PointerEventData @from, PointerEventData @to) {
            @to.position = @from.position;
            @to.delta = @from.delta;
            @to.scrollDelta = @from.scrollDelta;
            @to.pointerCurrentRaycast = @from.pointerCurrentRaycast;
            @to.pointerEnter = @from.pointerEnter;
        }

        //Controller stuff

        public override bool IsModuleSupported() {
            return forceModuleActive || ControllerManager.isInstantiated;
        }

        public override bool ShouldActivateModule() {
            if(!base.ShouldActivateModule())
                return false;

            if(forceModuleActive)
                return true;
            
            if(ControllerManager.instance.activeCount > 0)
                return true;

            return false;
        }

        public override void Process() {
            var ctrlMgr = ControllerManager.instance;
            
            for(int i = 0; i < ctrlMgr.activeCount; ++i) {
                var ctrl = ctrlMgr.GetActive(i);

                if(!ctrl.isDeviceAvailable)
                    continue;

                bool released;
                bool pressed;
                var pointer = GetControllerPointerEventData(ctrl, out pressed, out released);

                ProcessPress(pointer, pressed, released);

                if(!released) {
                    ProcessMove(pointer);
                    ProcessDrag(pointer);
                }
                else
                    RemovePointerData(pointer);
            }
        }

        protected void ProcessPress(PointerEventData pointerEvent, bool pressed, bool released) {
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if(pressed) {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if(pointerEvent.pointerEnter != currentOverGo) {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                if(newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // Debug.Log("Pressed: " + newPressed);

                float time = Time.unscaledTime;

                if(newPressed == pointerEvent.lastPress) {
                    var diffTime = time - pointerEvent.clickTime;
                    if(diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                }
                else {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if(pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
            }

            // PointerUp notification
            if(released) {
                // Debug.Log("Executing pressup on: " + pointer.pointerPress);
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                // Debug.Log("KeyCode: " + pointer.eventData.keyCode);

                // see if we mouse up on the same element that we clicked on...
                var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // PointerClick and Drop events
                if(pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick) {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                }
                else if(pointerEvent.pointerDrag != null && pointerEvent.dragging) {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if(pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                if(pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.pointerDrag = null;

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;
            }
        }

        public override void DeactivateModule() {
            base.DeactivateModule();
            ClearSelection();
        }

        public override string ToString() {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Input: M8 VR Controller");
            foreach(var pointerEventData in mPointerData)
                sb.AppendLine(pointerEventData.ToString());
            return sb.ToString();
        }
    }
}