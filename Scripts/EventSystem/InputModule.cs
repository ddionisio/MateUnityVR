using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace M8.VR.EventSystems {
    /// <summary>
    /// Use this to filter Unity's Event System with the Controllers.
    /// Ensure you add RayCasts only specific to VR
    /// </summary>
    [AddComponentMenu("M8/VR Event/InputModule")]
    public class InputModule : BaseInputModule {
        [SerializeField]
        bool _forceModuleActive;

        public bool forceModuleActive {
            get { return _forceModuleActive; }
            set { _forceModuleActive = value; }
        }

        public ControlMap leftClick = ControlMap.Trigger;
        public LayerMask layerMask;
        
        public float dragThreshold = 0.1f;

        /// <summary>
        /// Enter object has changed, provides previous enter object.
        /// </summary>
        public event Action<Pointer3DEventData, GameObject> enterChangedCallback;

        public static InputModule instance { get { return mInstance; } }

        /// <summary>
        /// key = id, value = PointerEventData
        /// </summary>
        protected Dictionary<int, Pointer3DEventData> mPointerData = new Dictionary<int, Pointer3DEventData>();

        private static InputModule mInstance;

        protected bool GetPointerData(int id, out Pointer3DEventData data, bool create) {
            if(!mPointerData.TryGetValue(id, out data) && create) {
                data = new Pointer3DEventData(eventSystem) {
                    pointerId = id,
                };
                mPointerData.Add(id, data);
                return true;
            }
            return false;
        }

        protected void RemovePointerData(Pointer3DEventData data) {
            mPointerData.Remove(data.pointerId);
        }
        
        protected Pointer3DEventData GetControllerPointerEventData(Controller ctrl, out bool pressed, out bool released) {
            Pointer3DEventData pointerData;
            GetPointerData(ctrl.deviceID, out pointerData, true);

            pointerData.Reset();

            pressed = ctrl.GetButtonPressed(leftClick);
            released = ctrl.GetButtonReleased(leftClick);
            
            pointerData.button = PointerEventData.InputButton.Left;

            //ensure there is at least one proper ray caster for this
            eventSystem.RaycastAll(pointerData, m_RaycastResultCache);

            var raycast = FindFirstRaycast(m_RaycastResultCache);            
            m_RaycastResultCache.Clear();

            //compute 2D position and delta
            if(raycast.gameObject) {
                var canvasHit = raycast.gameObject.GetComponentInParent<Canvas>();
                if(canvasHit)
                    canvasHit = canvasHit.rootCanvas;
                
                //grab 2D position
                if(pointerData.canvas == canvasHit && canvasHit) {
                    // generate position/delta for pointer
                    if(pressed)
                        pointerData.delta = Vector2.zero;
                    else
                        pointerData.delta = raycast.screenPosition - pointerData.position;
                    //
                }
                else {
                    pointerData.canvas = canvasHit;
                    pointerData.delta = Vector2.zero;
                }

                pointerData.position = raycast.screenPosition;
            }
            else
                pointerData.delta = Vector2.zero;
            //

            //process world delta
            if(pressed) {
                pointerData.pointerPressRaycast = raycast;
                pointerData.worldDelta = Vector3.zero;
            }
            else {
                if(raycast.gameObject && pointerData.pointerCurrentRaycast.gameObject)
                    pointerData.worldDelta = raycast.worldPosition - pointerData.pointerCurrentRaycast.worldPosition;
                else
                    pointerData.worldDelta = Vector3.zero;
            }
            //

            pointerData.pointerCurrentRaycast = raycast;

            return pointerData;
        }

        protected void ClearSelection() {
            var baseEventData = GetBaseEventData();

            foreach(var pointer in mPointerData.Values) {
                // clear all selection
                HandlePointerExitAndEnter(pointer, null);
            }

            mPointerData.Clear();
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

        protected Pointer3DEventData GetLastPointerEventData(int id) {
            Pointer3DEventData data;
            GetPointerData(id, out data, false);
            return data;
        }

        private static bool ShouldStartDrag(Pointer3DEventData ptr, float threshold) {
            if(!ptr.useDragThreshold)
                return true;

            if(ptr.canvas)
                return (ptr.pressPosition - ptr.position).sqrMagnitude >= threshold * threshold;

            if(ptr.pointerPressRaycast.gameObject && ptr.pointerCurrentRaycast.gameObject)
                return (ptr.pointerPressRaycast.worldPosition - ptr.pointerCurrentRaycast.worldPosition).sqrMagnitude >= threshold * threshold;

            return false;
        }

        protected virtual void ProcessMove(Pointer3DEventData pointerEvent) {
            var lastPointerEnter = pointerEvent.pointerEnter;

            var targetGO = (Cursor.lockState == CursorLockMode.Locked ? null : pointerEvent.pointerCurrentRaycast.gameObject);
            HandlePointerExitAndEnter(pointerEvent, targetGO);

            if(pointerEvent.pointerEnter != lastPointerEnter) {
                if(enterChangedCallback != null)
                    enterChangedCallback(pointerEvent, lastPointerEnter);
            }
        }

        protected virtual void ProcessDrag(Pointer3DEventData pointerEvent) {
            if(!pointerEvent.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                pointerEvent.pointerDrag == null)
                return;

            if(!pointerEvent.dragging
                && ShouldStartDrag(pointerEvent, dragThreshold)) {
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

        protected void CopyFromTo(Pointer3DEventData @from, Pointer3DEventData @to) {
            @to.position = @from.position;
            @to.delta = @from.delta;
            @to.worldDelta = @from.worldDelta;
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
            
            if(ControllerManager.instance.activeControllerCount > 0)
                return true;

            return false;
        }

        public override void Process() {
            var ctrlMgr = ControllerManager.instance;
            
            for(int i = 0; i < ctrlMgr.activeControllerCount; ++i) {
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

        protected void ProcessPress(Pointer3DEventData pointerEvent, bool pressed, bool released) {
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
                    var lastPointerEnter = pointerEvent.pointerEnter;

                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;

                    if(enterChangedCallback != null)
                        enterChangedCallback(pointerEvent, lastPointerEnter);
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
                // Debug.Log("Executing pressup on: " + pointerEvent.pointerPress);
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

                // redo pointer enter / exit to refresh state
                // so that if we moused over something that ignored it before
                // due to having pressed on something else
                // it now gets it.
                if(currentOverGo != pointerEvent.pointerEnter) {
                    var lastPointerEnter = pointerEvent.pointerEnter;

                    HandlePointerExitAndEnter(pointerEvent, null);
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);

                    if(pointerEvent.pointerEnter != lastPointerEnter) {
                        if(enterChangedCallback != null)
                            enterChangedCallback(pointerEvent, lastPointerEnter);
                    }
                }
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

        protected override void OnDestroy() {
            base.OnDestroy();

            if(mInstance == this)
                mInstance = null;
        }

        protected override void Awake() {
            if(mInstance == null)
                mInstance = this;

            base.Awake();
        }
    }
}