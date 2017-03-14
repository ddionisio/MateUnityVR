//=============================================================================
// Based on parts of Player.cs
// Copyright (c) Valve Corporation, All rights reserved.
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    [AddComponentMenu("M8/VR/ControllerManager")]
    public class ControllerManager : SingletonBehaviour<ControllerManager> {
        [Tooltip("List of possible Controllers, including no-SteamVR fallback Controllers.")]
        public Controller[] controllers;

        /// <summary>
        /// Get the number of active Controllers.
        /// </summary>
        public int activeCount {
            get {
                int count = 0;
                for(int i = 0; i < controllers.Length; i++) {
                    if(controllers[i].gameObject.activeInHierarchy) {
                        count++;
                    }
                }
                return count;
            }
        }
        
        //-------------------------------------------------
        public Controller leftHand {
            get {
                for(int j = 0; j < controllers.Length; j++) {
                    if(!controllers[j].gameObject.activeInHierarchy) {
                        continue;
                    }

                    if(controllers[j].CurrentHand() != Controller.Hand.Left) {
                        continue;
                    }

                    return controllers[j];
                }

                return null;
            }
        }


        //-------------------------------------------------
        public Controller rightHand {
            get {
                for(int j = 0; j < controllers.Length; j++) {
                    if(!controllers[j].gameObject.activeInHierarchy) {
                        continue;
                    }

                    if(controllers[j].CurrentHand() != Controller.Hand.Right) {
                        continue;
                    }

                    return controllers[j];
                }

                return null;
            }
        }

        /// <summary>
        /// Get the i-th active Controller. Starting at 0
        /// </summary>
        public Controller GetActive(int i) {
            for(int j = 0; j < controllers.Length; j++) {
                if(!controllers[j].gameObject.activeInHierarchy) {
                    continue;
                }

                if(i > 0) {
                    i--;
                    continue;
                }

                return controllers[j];
            }

            return null;
        }

        public Controller GetActiveByDeviceID(int id) {
            for(int i = 0; i < controllers.Length; i++) {
                if(!controllers[i].isDeviceAvailable)
                    continue;

                if(!controllers[i].gameObject.activeInHierarchy)
                    continue;

                if(controllers[i].deviceID == id)
                    return controllers[i];
            }

            return null;
        }
        
        protected override void OnInstanceDeinit() {
            
        }

        protected override void OnInstanceInit() {
            
        }
    }
}