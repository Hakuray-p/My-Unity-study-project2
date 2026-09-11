using UnityEngine;

[CreateAssetMenu(fileName = "HDAdventureConfig", menuName = "RX-未知裂痕/HD Adventure Config")]
// HD 冒险场景的外观与布局配置，做成资源文件方便在 Project 里调
public class HDAdventureConfig : ScriptableObject
{
    public Texture2D mapTexture; // 地图贴图
    public Texture2D gridTexture; // 网格贴图
    public Texture2D playerTexture; // 玩家标记贴图
    public Vector3 playerStartPosition = Vector3.zero; // 玩家起点
    public Vector3 entryOffset = new Vector3(0f, 0f, 4f); // 进入场景时的位置偏移
    public float cellSpacing = 86f; // 格子间距
    public Vector2 mapPanelSize = new Vector2(920f, 560f); // 地图面板尺寸
    public float freeMoveSpeed = 4f; // 自由移动速度
}
