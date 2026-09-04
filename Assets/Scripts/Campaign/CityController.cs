using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared city exploration runtime. It deliberately uses a placeholder avatar until character art is imported.
/// </summary>
public class CityController : MonoBehaviour
{
    public string cityId = "first_light";
    public Vector3 playerStart = new Vector3(0f, 1f, -7f);
    public float moveSpeed = 5f;

    private CharacterController playerController;
    private Transform player;
    private Camera cityCamera;
    private CityMatchPoint nearestPoint;
    private readonly List<CityMatchPoint> matchPoints = new List<CityMatchPoint>();

    private void Start()
    {
        CampaignSession.Instance.State.currentCityId = cityId;
        CampaignSession.Instance.Save();
        SetupCamera();
        CreatePlaceholderPlayer();
        matchPoints.AddRange(FindObjectsOfType<CityMatchPoint>());
        if (CampaignSession.Instance.State.currentCityId == cityId &&
            CampaignSession.Instance.State.playerPosition.y > 0.1f)
        {
            player.position = CampaignSession.Instance.State.playerPosition;
        }
    }

    private void SetupCamera()
    {
        cityCamera = Camera.main;
        if (cityCamera == null)
        {
            GameObject cameraObject = new GameObject("City Camera");
            cameraObject.tag = "MainCamera";
            cityCamera = cameraObject.AddComponent<Camera>();
        }

        cityCamera.orthographic = true;
        cityCamera.orthographicSize = 9f;
    }

    private void CreatePlaceholderPlayer()
    {
        GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        avatar.name = "Player Avatar (placeholder)";
        avatar.transform.position = playerStart;
        avatar.transform.localScale = new Vector3(0.8f, 1.1f, 0.8f);
        avatar.GetComponent<Renderer>().material.color = new Color(0.2f, 0.75f, 0.92f);
        Object.Destroy(avatar.GetComponent<Collider>());
        playerController = avatar.AddComponent<CharacterController>();
        playerController.height = 2f;
        playerController.radius = 0.45f;
        player = avatar.transform;
    }

    private void Update()
    {
        if (playerController == null) return;
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();
        playerController.Move(input * moveSpeed * Time.deltaTime);
        if (cityCamera != null && player != null)
        {
            cityCamera.transform.position = player.position + new Vector3(9f, 12f, -9f);
            cityCamera.transform.LookAt(player.position);
        }

        nearestPoint = null;
        float bestDistance = 3f;
        foreach (CityMatchPoint point in matchPoints)
        {
            if (point == null) continue;
            float distance = Vector3.Distance(player.position, point.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearestPoint = point;
            }
        }

        if (nearestPoint != null && Input.GetKeyDown(KeyCode.E)) TryStartMatch(nearestPoint.matchId);
        if (Input.GetKeyDown(KeyCode.Escape)) SceneFlowService.LoadWorldMap();
    }

    public void TryStartMatch(string matchId)
    {
        MatchData match = CampaignCatalog.GetMatch(matchId);
        if (match != null && CampaignSession.Instance.CanStartMatch(match))
        {
            CampaignSession.Instance.SetPlayerPosition(player != null ? player.position : playerStart);
            SceneFlowService.StartMatch(matchId, player != null ? player.position : playerStart);
        }
    }

    private void OnGUI()
    {
        CityData city = CampaignCatalog.GetCity(cityId);
        CitySaveData cityState = CampaignSession.Instance.GetCityState(cityId);
        GUI.Box(new Rect(20f, 18f, 390f, 108f), city != null ? city.displayName : cityId);
        GUI.Label(new Rect(38f, 48f, 350f, 22f), $"League points: {cityState.leaguePoints}/{(city != null ? city.requiredPoints : 0)}");
        GUI.Label(new Rect(38f, 74f, 350f, 22f), "WASD / arrows: explore     E: talk and play");
        if (GUI.Button(new Rect(38f, 98f, 150f, 22f), "World map")) SceneFlowService.LoadWorldMap();

        float y = 145f;
        if (city != null)
        {
            foreach (string matchId in city.availableMatchIds)
            {
                MatchData match = CampaignCatalog.GetMatch(matchId);
                if (match == null) continue;
                bool canStart = CampaignSession.Instance.CanStartMatch(match);
                string label = match.displayName + (CampaignSession.Instance.IsMatchComplete(matchId) ? " [DONE]" : "");
                GUI.enabled = canStart;
                if (GUI.Button(new Rect(38f, y, 330f, 28f), label)) TryStartMatch(matchId);
                GUI.enabled = true;
                y += 34f;
            }
        }

        if (nearestPoint != null)
        {
            GUI.Label(new Rect(400f, 20f, 360f, 28f), "E  " + nearestPoint.displayName);
        }
    }

    private void OnDisable()
    {
        if (player != null && CampaignSession.Instance.State != null)
        {
            CampaignSession.Instance.SetPlayerPosition(player.position);
            CampaignSession.Instance.Save();
        }
    }
}

public class CityMatchPoint : MonoBehaviour
{
    public string matchId;
    public string displayName;

    private void OnMouseDown()
    {
        CityController city = FindObjectOfType<CityController>();
        if (city != null) city.TryStartMatch(matchId);
    }
}
