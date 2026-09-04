using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight world-map presentation. City travel remains data-driven by CampaignCatalog.
/// </summary>
public class WorldMapController : MonoBehaviour
{
    private Camera mapCamera;
    private readonly Dictionary<string, Vector3> cityPositions = new Dictionary<string, Vector3>
    {
        { "first_light", new Vector3(-5f, 0f, 0f) },
        { "glass_harbor", new Vector3(5f, 0f, 0f) }
    };

    private void Start()
    {
        CampaignSession.Instance.ToString();
        SetupMapView();
    }

    private void SetupMapView()
    {
        mapCamera = Camera.main;
        if (mapCamera == null)
        {
            GameObject cameraObject = new GameObject("WorldMap Camera");
            cameraObject.tag = "MainCamera";
            mapCamera = cameraObject.AddComponent<Camera>();
        }

        mapCamera.orthographic = true;
        mapCamera.orthographicSize = 8f;
        mapCamera.transform.position = new Vector3(0f, 12f, -8f);
        mapCamera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        CreateMapGeometry();
    }

    private void CreateMapGeometry()
    {
        if (GameObject.Find("Map Ground") != null) return;
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "World Map Ground";
        ground.transform.localScale = new Vector3(2.5f, 1f, 1.5f);
        ground.GetComponent<Renderer>().material.color = new Color(0.16f, 0.27f, 0.24f);

        GameObject route = GameObject.CreatePrimitive(PrimitiveType.Cube);
        route.name = "League Route";
        route.transform.position = Vector3.up * 0.15f;
        route.transform.localScale = new Vector3(10f, 0.18f, 0.45f);
        route.GetComponent<Renderer>().material.color = new Color(0.76f, 0.58f, 0.31f);

        foreach (CityData city in CampaignCatalog.Cities)
        {
            Vector3 position = cityPositions.ContainsKey(city.cityId) ? cityPositions[city.cityId] : Vector3.zero;
            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            node.name = "City Node " + city.cityId;
            node.transform.position = position + Vector3.up * 0.55f;
            node.transform.localScale = new Vector3(1.15f, 0.55f, 1.15f);
            node.GetComponent<Renderer>().material.color = CampaignSession.Instance.IsCityUnlocked(city.cityId)
                ? new Color(0.25f, 0.78f, 0.72f)
                : new Color(0.2f, 0.22f, 0.24f);
            CityMapNode mapNode = node.AddComponent<CityMapNode>();
            mapNode.cityId = city.cityId;
        }
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(24f, 20f, 360f, 100f), "LEAGUE JOURNEY");
        GUI.Label(new Rect(42f, 48f, 330f, 24f), "Travel between cities and collect championship points.");

        float y = 135f;
        foreach (CityData city in CampaignCatalog.Cities)
        {
            bool unlocked = CampaignSession.Instance.IsCityUnlocked(city.cityId);
            string label = unlocked ? "Enter " + city.displayName : city.displayName + " (LOCKED)";
            if (GUI.Button(new Rect(42f, y, 300f, 34f), label) && unlocked)
            {
                SceneFlowService.EnterCity(city.cityId);
            }
            y += 44f;
        }

        if (CampaignSession.Instance.State.pendingBattle != null &&
            GUI.Button(new Rect(42f, y + 8f, 300f, 34f), "Resume pending match"))
        {
            SceneFlowService.ResumePendingBattle();
        }
    }
}

public class CityMapNode : MonoBehaviour
{
    public string cityId;

    private void OnMouseDown()
    {
        if (CampaignSession.Instance.IsCityUnlocked(cityId)) SceneFlowService.EnterCity(cityId);
    }
}
