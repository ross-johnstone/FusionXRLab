using UnityEngine;
using Ubiq.Messaging;
using System.Collections.Generic;

public class DarknessController : MonoBehaviour
{
    private bool lightsEnabled = true;
    private Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();
    private NetworkContext context;
    private Dictionary<GameObject, Material[]> originalSwitchMaterials = new Dictionary<GameObject, Material[]>();

    void Start()
    {
        // Register for network messages
        context = NetworkScene.Register(this);
        Debug.Log($"[DarknessController] Network: Registered with ID {context.Id}");
    }

    public void ToggleDarkness()
    {
        try
        {
            lightsEnabled = !lightsEnabled;
            Debug.Log($"[DarknessController] Lights {(lightsEnabled ? "ON" : "OFF")}");

            // Send the new state to all clients
            if (context.Id.Valid)
            {
                var message = new DarknessMessage
                {
                    lightsEnabled = lightsEnabled
                };
                context.SendJson(message);
                Debug.Log($"[DarknessController] Network: Sent darkness state to all clients");
            }
            else
            {
                Debug.LogWarning("[DarknessController] Network: Context invalid - cannot send state");
            }

            // Update local state
            UpdateDarknessState();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DarknessController] Error toggling darkness: {e.Message}\n{e.StackTrace}");
        }
    }

    private struct DarknessMessage
    {
        public bool lightsEnabled;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        Debug.Log("[DarknessController] Network: Received darkness state update");
        var msg = message.FromJson<DarknessMessage>();
        lightsEnabled = msg.lightsEnabled;
        UpdateDarknessState();
    }

    private void UpdateDarknessState()
    {
        // Toggle all lights in the scene
        ToggleLights();

        int totalObjectsChanged = 0;

        // Process XRLabRoomC Walls & Lights children
        GameObject xrLabRoom = GameObject.Find("XRLabRoomC Walls & Lights");
        if (xrLabRoom != null)
        {
            // Handle divider ends specifically
            GameObject dividerEnds = xrLabRoom.transform.Find("SM_XR_DividerEnds_01_10")?.gameObject;
            if (dividerEnds != null)
            {
                HandleBasicWhiteMaterial(dividerEnds, lightsEnabled);
                totalObjectsChanged++;
            }

            // Process other children
            ProcessChildObjects(xrLabRoom, obj => {
                if (obj.name != "SM_XR_DividerEnds_01_10") // Skip divider ends as they're handled separately
                {
                    ScaleMaterialColor(obj, lightsEnabled ? 1f : 0.05f); // 95% darker
                    totalObjectsChanged++;
                }
            });
        }

        // Process wall lines
        GameObject linesParent = GameObject.Find("Lines");
        if (linesParent != null)
        {
            ProcessChildObjects(linesParent, line => {
                if (line != null)
                {
                    foreach (Renderer renderer in line.GetComponentsInChildren<Renderer>())
                    {
                        if (renderer != null)
                        {
                            Color targetColor = lightsEnabled ? Color.white : new Color(0.05f, 0.05f, 0.05f); // 95% darker
                            renderer.material.color = targetColor;
                            totalObjectsChanged++;
                        }
                    }
                }
            });
        }

        // Handle specific objects with their materials
        GameObject emissiveLight = GameObject.Find("SM_XR_Lights_01_0");
        HandleEmissiveMaterial(emissiveLight, "M_EmissiveLight", 
            new Color(1f, 167f/255f, 28f/255f), Color.black);
        if (emissiveLight != null) totalObjectsChanged++;

        GameObject floor = GameObject.Find("SM_XR_Floor_01_44");
        HandleSpecificMaterial(floor, "M_Floor",
            new Color(188f/255f, 190f/255f, 190f/255f), new Color(0.05f, 0.05f, 0.05f)); // 95% darker
        if (floor != null) totalObjectsChanged++;

        GameObject wall = GameObject.Find("SM_XR_Walls_01_52");
        HandleSpecificMaterial(wall, "M_Wall",
            new Color(255f/255f, 254f/255f, 242f/255f), new Color(0.05f, 0.05f, 0.05f)); // 95% darker
        if (wall != null) totalObjectsChanged++;

        // Process Hololens objects
        GameObject cleanBoxsParent = GameObject.Find("CleanBoxs");
        if (cleanBoxsParent != null)
        {
            foreach (Transform child in cleanBoxsParent.GetComponentsInChildren<Transform>())
            {
                if (child.gameObject.name == "Hololens")
                {
                    HandleEmissiveMaterial(child.gameObject, "M_General_ORM",
                        Color.white, new Color(0.05f, 0.05f, 0.05f)); // 95% darker
                    totalObjectsChanged++;
                }
            }
        }

        // Process Switches
        GameObject switchesParent = GameObject.Find("Switches");
        if (switchesParent != null)
        {
            ProcessChildObjects(switchesParent, switchObj => {
                HandleSwitchMaterials(switchObj, lightsEnabled);
                totalObjectsChanged++;
            });
        }

        // Process Sticker object
        GameObject sticker = GameObject.Find("Sticker");
        if (sticker != null)
        {
            ScaleMaterialColor(sticker, lightsEnabled ? 1f : 0.05f); // 95% darker
            totalObjectsChanged++;
        }

        Debug.Log($"[DarknessController] Changed {totalObjectsChanged} objects to {(lightsEnabled ? "light" : "dark")} mode");
    }

    private void ToggleLights()
    {
        var roomLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in roomLights)
        {
            if (light != null)
            {
                light.enabled = lightsEnabled;
            }
        }
    }

    private void ScaleMaterialColor(GameObject obj, float scaleFactor)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.materials != null)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat != null)
                {
                    try
                    {
                        // Store original color if we haven't seen this material before
                        if (!originalColors.ContainsKey(mat))
                        {
                            // Try different color property names
                            if (mat.HasProperty("_BaseColor"))
                            {
                                originalColors[mat] = mat.GetColor("_BaseColor");
                            }
                            else if (mat.HasProperty("_Color"))
                            {
                                originalColors[mat] = mat.GetColor("_Color");
                            }
                            else
                            {
                                // Skip materials without color properties
                                continue;
                            }
                        }

                        // Get the color to apply (either scaled down or original)
                        Color colorToApply = scaleFactor < 1f ? 
                            originalColors[mat] * scaleFactor : // Scale down
                            originalColors[mat]; // Restore original

                        // Apply color using available properties
                        if (mat.HasProperty("_BaseColor"))
                        {
                            mat.SetColor("_BaseColor", colorToApply);
                        }
                        if (mat.HasProperty("_Color"))
                        {
                            mat.SetColor("_Color", colorToApply);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[DarknessController] Could not process material {mat.name} on {obj.name}: {e.Message}");
                    }
                }
            }
        }
    }

    private void HandleEmissiveMaterial(GameObject obj, string materialName, Color enabledColor, Color disabledColor)
    {
        if (obj == null) return;
        
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material[] materials = renderer.materials;
            foreach (Material mat in materials)
            {
                if (mat != null && mat.name.Contains(materialName))
                {
                    Color emissiveColor = lightsEnabled ? enabledColor : disabledColor;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissiveColor);
                    break;
                }
            }
        }
    }

    private void HandleSpecificMaterial(GameObject obj, string materialName, Color enabledColor, Color disabledColor)
    {
        if (obj == null) return;
        
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material[] materials = renderer.materials;
            foreach (Material mat in materials)
            {
                if (mat != null && mat.name.Contains(materialName))
                {
                    Color color = lightsEnabled ? enabledColor : disabledColor;
                    mat.SetColor("_BaseColor", color);
                    mat.SetColor("_Color", color);
                    break;
                }
            }
        }
    }

    private void ProcessChildObjects(GameObject parent, System.Action<GameObject> action)
    {
        if (parent != null)
        {
            foreach (Transform child in parent.transform)
            {
                action(child.gameObject);
            }
        }
    }

    private void HandleSwitchMaterials(GameObject obj, bool enable)
    {
        if (obj == null) return;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!originalSwitchMaterials.ContainsKey(obj))
            {
                // Store a copy of the original materials
                Material[] originalMats = renderer.materials;
                Material[] matsCopy = new Material[originalMats.Length];
                for (int i = 0; i < originalMats.Length; i++)
                {
                    matsCopy[i] = new Material(originalMats[i]);
                }
                originalSwitchMaterials[obj] = matsCopy;
            }

            if (enable)
            {
                // Restore the original materials
                renderer.materials = originalSwitchMaterials[obj];
            }
            else
            {
                // Darken the materials (but do not overwrite the originals)
                Material[] darkMats = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    Material mat = new Material(renderer.materials[i]);
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", mat.GetColor("_BaseColor") * 0.2f);
                    }
                    if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", mat.GetColor("_Color") * 0.2f);
                    }
                    darkMats[i] = mat;
                }
                renderer.materials = darkMats;
            }
        }
    }

    private void HandleBasicWhiteMaterial(GameObject obj, bool enable)
    {
        if (obj == null) return;
        
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat != null && mat.name.Contains("BasicWhite"))
                {
                    Color colorToApply = enable ? Color.white : new Color(0.2f, 0.2f, 0.2f);
                    
                    // Create a new material instance
                    Material newMat = new Material(mat);
                    newMat.SetColor("_BaseColor", colorToApply);
                    newMat.SetColor("_Color", colorToApply);
                    materials[i] = newMat;
                }
            }
            renderer.materials = materials;
        }
    }
} 