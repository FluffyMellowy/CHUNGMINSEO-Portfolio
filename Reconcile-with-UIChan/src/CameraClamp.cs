using UnityEngine;

/// <summary>
/// オルソカメラの矩形領域内に座標をクランプする共通ロジック。
/// WindowFrame の追従処理と Minigame1Manager のリスポーン位置算出で重複していたものを集約。
/// </summary>
public static class CameraClamp
{
    /// <summary>
    /// 指定の位置を、オルソカメラの可視矩形から半サイズぶん内側にクランプして返す。
    /// オフセットを指定するとクランプ範囲を上下左右に緩和できる（タイトルバーを画面外に出す用途等）。
    /// 戻り値の Z はinputと同じ値をそのまま保持する。
    /// </summary>
    public static Vector3 ClampToCamera(
        Vector3 targetPos,
        Camera camera,
        float halfWidth,
        float halfHeight,
        float topOffset = 0f,
        float bottomOffset = 0f,
        float leftOffset = 0f,
        float rightOffset = 0f)
    {
        float camHeight = camera.orthographicSize;
        float camWidth = camHeight * camera.aspect;
        Vector3 camPos = camera.transform.position;

        float minX = camPos.x - camWidth + halfWidth - leftOffset;
        float maxX = camPos.x + camWidth - halfWidth + rightOffset;
        float minY = camPos.y - camHeight + halfHeight - bottomOffset;
        float maxY = camPos.y + camHeight - halfHeight + topOffset;

        float clampedX = Mathf.Clamp(targetPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPos.y, minY, maxY);

        return new Vector3(clampedX, clampedY, targetPos.z);
    }
}
