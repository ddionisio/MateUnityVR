//=============================================================================
// Based on SpawnRenderModel.cs
// Copyright (c) Valve Corporation, All rights reserved.
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Valve.VR;

namespace M8.VR {
    /// <summary>
    /// Put this on the spawn prefab of the Controller, or within the Controller hierarchy
    /// </summary>
    [AddComponentMenu("M8/VR Steam/ControllerSpawnModel")]
    public class ControllerSpawnModelSteamVR : MonoBehaviour, IControllerInitialize {
        public ControllerSteamVR controller;

        public Material[] materials;

        private SteamVR_RenderModel[] renderModels;
        private List<MeshRenderer> renderers = new List<MeshRenderer>();

        private static List<ControllerSpawnModelSteamVR> spawnRenderModels = new List<ControllerSpawnModelSteamVR>();
        private static int lastFrameUpdated;
        private static int spawnRenderModelUpdateIndex;

        SteamVR_Events.Action renderModelLoadedAction;


        //-------------------------------------------------
        void Awake() {
            renderModels = new SteamVR_RenderModel[materials.Length];
            renderModelLoadedAction = SteamVR_Events.RenderModelLoadedAction(OnRenderModelLoaded);
        }


        //-------------------------------------------------
        void OnEnable() {
            ShowController();

            renderModelLoadedAction.enabled = true;

            spawnRenderModels.Add(this);
        }


        //-------------------------------------------------
        void OnDisable() {
            HideController();

            renderModelLoadedAction.enabled = false;

            spawnRenderModels.Remove(this);
        }
        
        //-------------------------------------------------
        void Update() {
            // Only update one per frame
            if(lastFrameUpdated == Time.renderedFrameCount) {
                return;
            }
            lastFrameUpdated = Time.renderedFrameCount;


            // SpawnRenderModel overflow
            if(spawnRenderModelUpdateIndex >= spawnRenderModels.Count) {
                spawnRenderModelUpdateIndex = 0;
            }


            // Perform update
            if(spawnRenderModelUpdateIndex < spawnRenderModels.Count) {
                SteamVR_RenderModel renderModel = spawnRenderModels[spawnRenderModelUpdateIndex].renderModels[0];
                if(renderModel != null) {
                    renderModel.UpdateComponents(OpenVR.RenderModels);
                }
            }

            spawnRenderModelUpdateIndex++;
        }


        //-------------------------------------------------
        private void ShowController() {
            if(controller == null || !controller.isDeviceAvailable) {
                return;
            }

            for(int i = 0; i < renderModels.Length; i++) {
                if(renderModels[i] == null) {
                    renderModels[i] = new GameObject("SteamVR_RenderModel").AddComponent<SteamVR_RenderModel>();
                    renderModels[i].updateDynamically = false; // Update one per frame (see Update() method)
                    renderModels[i].transform.SetParent(transform, false);
                    renderModels[i].transform.localPosition = Vector3.zero;
                    renderModels[i].transform.localRotation = Quaternion.identity;
                    renderModels[i].transform.localScale = Vector3.one;
                }

                renderModels[i].gameObject.SetActive(true);
                renderModels[i].SetDeviceIndex((int)controller.device.index);
            }
        }


        //-------------------------------------------------
        private void HideController() {
            for(int i = 0; i < renderModels.Length; i++) {
                if(renderModels[i] != null) {
                    renderModels[i].gameObject.SetActive(false);
                }
            }
        }


        //-------------------------------------------------
        private void OnRenderModelLoaded(SteamVR_RenderModel renderModel, bool success) {
            for(int i = 0; i < renderModels.Length; i++) {
                if(renderModel == renderModels[i]) {
                    if(materials[i] != null) {
                        renderers.Clear();
                        renderModels[i].GetComponentsInChildren<MeshRenderer>(renderers);
                        for(int j = 0; j < renderers.Count; j++) {
                            Texture mainTexture = renderers[j].material.mainTexture;
                            renderers[j].sharedMaterial = materials[i];
                            renderers[j].material.mainTexture = mainTexture;
                        }
                    }
                }
            }
        }

        void IControllerInitialize.OnInitialized(Controller ctrl) {            
            controller = (ControllerSteamVR)ctrl;
            ShowController();
        }
    }
}