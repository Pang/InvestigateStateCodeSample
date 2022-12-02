public static class HelperMethods
{
    public static bool CheckInRange(Vector3 targetPos, Vector3 fromPos, float radius)
    {
        // Displace position & check distance to 0,0
        float magnitude = (targetPos - fromPos).magnitude;
        return magnitude < radius;
    }

    public static bool CheckInSight(Vector3 target, Transform tSelf, float radius, float peripheralVisionDegrees)
    {
        Vector3 dotTarget = (target - tSelf.position).normalized;
        float dotProduct = Vector3.Dot(dotTarget, tSelf.forward);

        if (dotProduct > 0)
        {
            // Check target is within peripheral vision
            float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;
            
            if (angle < peripheralVisionDegrees)
            {
                // check vision not obscured from eye position to targets head
                RaycastHit hit;
                Vector3 sightLevel = tSelf.position + new Vector3(0, 3, 0);
                Vector3 targetLevel = target + new Vector3(0, 1.5f, 0);

                Physics.Linecast(sightLevel, targetLevel, out hit);
                if (hit.collider?.tag == "Player") return true;
            }
        }

        return false;
    }

    public static bool CheckInRangeAndSight(Vector3 targetPos, Transform tSelf, float radius, float peripheralVisionDegrees)
    {
        if (!CheckInRange(targetPos, tSelf.position, radius)) return false;
        return CheckInSight(targetPos, tSelf, radius, peripheralVisionDegrees);
    }

    public static void ShakeRotational(Transform transform, Vector2 coords, float lacunarity, float scale)
    {
        transform.eulerAngles = new Vector3(
            transform.eulerAngles.x,
            transform.eulerAngles.y,
            ((Mathf.PerlinNoise(coords.x, coords.y) - 0.5f) * lacunarity) * scale
        );
    }
}
