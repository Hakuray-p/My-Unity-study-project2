using UnityEngine;

[CreateAssetMenu(fileName = "HDAdventureConfig", menuName = "RX-未知裂痕/HD Adventure Config")]
public class HDAdventureConfig : ScriptableObject
{
    public Texture2D mapTexture;
    public Texture2D gridTexture;
    public Texture2D playerTexture;
    public Vector3 playerStartPosition = Vector3.zero;
    public Vector3 entryOffset = new Vector3(0f, 0f, 4f);
    public float cellSpacing = 86f;
    public Vector2 mapPanelSize = new Vector2(920f, 560f);
    public float freeMoveSpeed = 4f;
}
