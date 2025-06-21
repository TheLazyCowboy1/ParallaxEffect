using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Diagnostics;
using System.Collections;
using RWCustom;
using System.Linq;

namespace ParallaxEffect;

public partial class Plugin
{
    #region BatchRender

    //NOTE: the rooms include the camera pos; that is, they're "screens"
    public IEnumerator GenerateForAllRooms(List<string> rooms)
    {

        //CanRenderBackground = false; //nah; why block it from rendering; that's stupid
        Stopwatch sw = new();
        sw.Start();

        int MAX_QUEUE = (Options.ShaderLayers.Value >= 3) ? 4 : 8;
        int roomsRemaining = rooms.Count;
        int gensRemaining = Options.PreLoadRoomCap.Value;
        //int queueLeft = MAX_QUEUE;
        bool hasDisplayedMessage = false;

        bool[] isBusy = new bool[MAX_QUEUE];
        Texture2D[] tempLevelTex = new Texture2D[MAX_QUEUE];
        RenderTexture[] tempL2Tex = new RenderTexture[MAX_QUEUE], tempL3Tex = (Options.ShaderLayers.Value >= 3) ? new RenderTexture[MAX_QUEUE] : null;
        for (int i = 0; i < MAX_QUEUE; i++)
        {
            tempLevelTex[i] = MakeTex2D(1400, 800);
            tempL2Tex[i] = MakeRenderTex(1400, 800);
            if (Options.ShaderLayers.Value >= 3)
                tempL3Tex[i] = MakeRenderTex(1400, 800);
        }

        void maybeFinished()
        {
            if (roomsRemaining <= 0)
            {
                //CanRenderBackground = true;
                sw.Stop();
                Logger.LogDebug("Total time for batch render: " + (sw.ElapsedMilliseconds * 0.001f));
                TryDisplayMessage("Finished generating backgrounds.");

                for (int i = 0; i < MAX_QUEUE; i++)
                {
                    tempLevelTex[i] = null;
                    tempL2Tex[i].Release();
                    tempL2Tex[i] = null;
                    if (Options.ShaderLayers.Value >= 3)
                    {
                        tempL3Tex[i].Release();
                        tempL3Tex[i] = null;
                    }
                }
            }
        }

        //foreach (string room in rooms)
        for (int roomIdx = 0; roomIdx < rooms.Count; roomIdx++)
        {
            string room = rooms[roomIdx];
            if (!hasDisplayedMessage)
                hasDisplayedMessage = TryDisplayMessage("Generating backgrounds for all rooms in the region. This may take several minutes.", 3, 0.2f);

            if (gensRemaining <= 0)
            {
                roomsRemaining -= rooms.Count - roomIdx;
                maybeFinished();
                break;
            }
            //while (queueLeft <= 0) yield return null; //wait until there is a spot in the queue
            //while (!CanRenderBackground)
                //yield return null;

            while (!isBusy.Contains(false)) //wait until at least one slot isn't busy
                yield return null;

            int idx = 0;
            for (int i = 0; i < MAX_QUEUE; i++)
            {
                if (!isBusy[i]) { idx = i; break; }
            }

            try
            {
                //load tex
                string path = WorldLoader.FindRoomFile(room, false, ".png");
                if (!File.Exists(path))
                {
                    roomsRemaining--;
                    Logger.LogError("Couldn't find file for room " + room);
                    maybeFinished();
                    continue;//return;
                }

                //check if background needs to be made at all
                string l2Path = FindBackgroundTexture(room + "_l2"), l3Path = (Options.ShaderLayers.Value >= 3) ? FindBackgroundTexture(room + "_l3") : "t";
                if (l2Path != null && l3Path != null)
                {
                    roomsRemaining--;
                    maybeFinished();
                    continue; //the background textures already exist!
                }

                //CanRenderBackground = false;
                //queueLeft--;
                isBusy[idx] = true;
                gensRemaining--;

                tempLevelTex[idx].LoadImage(File.ReadAllBytes(path), false);
                tempLevelTex[idx].Apply();

                //resize render textures if needed
                if (tempL2Tex[idx].width != tempLevelTex[idx].width || tempL2Tex[idx].height != tempLevelTex[idx].height)
                {
                    tempL2Tex[idx].Release();
                    tempL2Tex[idx] = MakeRenderTex(tempLevelTex[idx].width, tempLevelTex[idx].height);
                    if (Options.ShaderLayers.Value >= 3)
                    {
                        tempL3Tex[idx].Release();
                        tempL3Tex[idx] = MakeRenderTex(tempLevelTex[idx].width, tempLevelTex[idx].height);
                    }
                }

                //create compute buffer for each room, and send it to the graphics queue

                CommandBuffer buffer = new() { name = "TheLazyCowboy1_BackgroundBuilder_" + room };

                //Load layer2 from file
                if (l2Path != null)
                {
                    Texture2D l2Tex2D = MakeTex2D(tempLevelTex[idx].width, tempLevelTex[idx].height);
                    l2Tex2D.LoadImage(File.ReadAllBytes(l2Path), false);
                    l2Tex2D.Apply();
                    buffer.Blit(l2Tex2D, tempL2Tex[idx]);
                }
                //generate layer 2
                else
                {
                    buffer.Blit(tempLevelTex[idx], tempL2Tex[idx], Layer2BuilderMaterial);
                    buffer.RequestAsyncReadback(tempL2Tex[idx], result =>
                    {
                        try
                        {
                            if (result.done)
                            {
                                if (result.hasError)
                                    Logger.LogError("Error generating layer 2 texture for " + room);
                                else
                                    SaveBackgroundTexture(result, room + "_l2");

                                if (Options.ShaderLayers.Value < 3)
                                {
                                    roomsRemaining--;
                                    //queueLeft++;
                                    isBusy[idx] = false;
                                    //CanRenderBackground = true;
                                    buffer.Release();
                                    //tempL2Tex.Release();
                                    maybeFinished();
                                }
                            }
                        }
                        catch (Exception ex) {
                            Logger.LogError(room);
                            Logger.LogError(ex);
                            roomsRemaining--;
                            //queueLeft++;
                            isBusy[idx] = false;
                            maybeFinished();
                        }
                    });
                }

                if (Options.ShaderLayers.Value >= 3)
                {
                    Material l3mat = new(BackgroundBuilderShader) { name = "TheLazyCowboy1_BackgroundBuilderMat_" + room + "_l3" };
                    l3mat.EnableKeyword("THELAZYCOWBOY1_INCLUDELAYER2");
                    l3mat.SetTexture("_Layer2Tex", tempL2Tex[idx]);

                    buffer.Blit(tempLevelTex[idx], tempL3Tex[idx], l3mat);
                    buffer.RequestAsyncReadback(tempL3Tex[idx], result =>
                    {
                        try
                        {
                            if (result.done)
                            {
                                if (result.hasError)
                                    Logger.LogError("Error generating layer 3 texture for " + room);
                                else
                                    SaveBackgroundTexture(result, room + "_l3");

                                roomsRemaining--;
                                //queueLeft++;
                                isBusy[idx] = false;
                                //CanRenderBackground = true;
                                buffer.Release();
                                maybeFinished();
                            }
                        }
                        catch (Exception ex) {
                            Logger.LogError(room);
                            Logger.LogError(ex);
                            roomsRemaining--;
                            //queueLeft++;
                            isBusy[idx] = false;
                            maybeFinished();
                        }
                    });
                }

                Graphics.ExecuteCommandBufferAsync(buffer, ComputeQueueType.Urgent);

                Logger.LogDebug($"Generating background images in slot {idx} for {room}");
            }
            catch (Exception ex)
            {
                roomsRemaining--;
                //queueLeft++;
                isBusy[idx] = false;
                Logger.LogError(room);
                Logger.LogError(ex);
                maybeFinished();
            }

        }

        //while (roomsRemaining > 0) Thread.Sleep(1);

        //Set CanRenderBackground to true as soon as the queue isn't full and all rooms have been queued
        //while (!isBusy.Contains(false)) yield return null;
        //CanRenderBackground = true;

    }

    private bool TryDisplayMessage(string msg, float time = 3, float delay = 0)
    {
        try
        {
            if (Custom.rainWorld?.processManager?.currentMainLoop is RainWorldGame game
                && game.cameras != null && game.cameras.Length > 0
                && game.cameras[0]?.hud?.textPrompt?.messages != null)
            {
                game.cameras[0].hud.textPrompt.AddMessage(msg, (int)(delay * 40), (int)(time * 40), false, ModManager.MMF);
                return true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
        return false;
    }

    public static RenderTexture MakeRenderTex(int width, int height) => new(width, height, 0, DefaultFormat.LDR) { filterMode = 0 };
    public static Texture2D MakeTex2D(int width, int height) => new(width, height, TextureFormat.ARGB32, false) { filterMode = 0, anisoLevel = 0, wrapMode = TextureWrapMode.Clamp };

    #endregion

    #region TextureSaving

    public void SaveBackgroundTexture(AsyncGPUReadbackRequest result, string room)
    {
        try
        {
            if (FindBackgroundTexture(room) != null)
                return; //already have a background texture for it!

            //file path for texture
            string path = Path.Combine(ModFolderPath, "world", RegionLayersFolder(room.Split('_')[0]));
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path += Path.DirectorySeparatorChar + room + ".png";

            //if (UseBackgroundTexture(path, room))
                //return; //already have a perfectly valid texture!
            if (Options.OverwriteBackgroundTextures.Value)
                GeneratedBackgrounds.Add(room);

            //Texture2D newTex = new(texture.width, texture.height);
            //Graphics.CopyTexture(texture, newTex);
            Texture2D tex = new(result.width, result.height);
            tex.SetPixelData<Color>(result.GetData<Color>(), 0);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Logger.LogDebug("Saved background " + path);
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }

    //NOTE: room includes the layer: SU_A23_1_l2
    //returns null if not found
    public string FindBackgroundTexture(string room)
    {
        try
        {
            //string path = Path.Combine(ModFolderPath, "world", room.Split('_')[0], );
            string path = AssetManager.ResolveFilePath(Path.Combine("world", RegionLayersFolder(room.Split('_')[0]), room + ".png"), true);
            //Logger.LogDebug("Trying to preload " + path);
            if (UseBackgroundTexture(path, room))
            {
                return path;
            }

            //backup: try the rooms folders
            path = WorldLoader.FindRoomFile(room, true, ".png");
            if (UseBackgroundTexture(path, room))
            {
                //Logger.LogDebug("Preloading background from rooms folder " + path);
                return path;
            }

        }
        catch (Exception ex) { Logger.LogError(ex); }
        return null;
    }
    public byte[] PreLoadBackgroundTexture(string room)
    {
        try
        {
            string path = FindBackgroundTexture(room);
            if (path != null)
            {
                Logger.LogDebug("Preloading background texture " + path);
                return File.ReadAllBytes(path);
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
        return null;
    }

    private static string RegionLayersFolder(string region) => region.ToUpper() == "GATE" ? "gates-Layers" : region + "-Layers";

    //Don't load textures if they don't exist (duh) or overwrite is true AND the file is more than a minute old
    private bool UseBackgroundTexture(string path, string room) => File.Exists(path) && (!Options.OverwriteBackgroundTextures.Value || GeneratedBackgrounds.Contains(room));// || DateTime.UtcNow.Subtract(File.GetCreationTimeUtc(path)).TotalMinutes < 1.0);
    #endregion
}
