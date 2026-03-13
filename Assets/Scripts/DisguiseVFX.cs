using System.Collections;
using UnityEngine;

/// <summary>
/// Creates a Sims-style spinning particle cloud effect around the player
/// during a disguise change. Uses Unity's built-in particle system to create
/// a swirling cloud of sparkles and dust that obscures the character.
/// </summary>
public class DisguiseVFX : MonoBehaviour
{
    [HideInInspector] public Transform targetTransform;
    [HideInInspector] public float duration = 2.5f;

    private ParticleSystem cloudParticles;
    private ParticleSystem sparkleParticles;
    private ParticleSystem dustRingParticles;

    public void StartEffect()
    {
        StartCoroutine(RunEffect());
    }

    private IEnumerator RunEffect()
    {
        CreateCloudParticles();
        CreateSparkleParticles();
        CreateDustRingParticles();

        // Start all particle systems
        cloudParticles.Play();
        sparkleParticles.Play();
        dustRingParticles.Play();

        // Spin the whole VFX object around the character
        float elapsed = 0f;
        float spinSpeed = 720f; // degrees per second

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Follow the target
            if (targetTransform != null)
            {
                transform.position = targetTransform.position + Vector3.up * 1f;
            }

            // Spin the effect
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            // Ramp up then down the spin speed for dramatic effect
            float t = elapsed / duration;
            if (t < 0.3f)
                spinSpeed = Mathf.Lerp(180f, 1080f, t / 0.3f);
            else if (t > 0.7f)
                spinSpeed = Mathf.Lerp(1080f, 180f, (t - 0.7f) / 0.3f);

            yield return null;
        }

        // Stop emission and let remaining particles fade
        cloudParticles.Stop();
        sparkleParticles.Stop();
        dustRingParticles.Stop();

        yield return new WaitForSeconds(1.5f);

        Destroy(gameObject);
    }

    private void CreateCloudParticles()
    {
        // Main swirling cloud
        GameObject cloudObj = new GameObject("Cloud");
        cloudObj.transform.SetParent(transform, false);
        cloudObj.transform.localPosition = Vector3.zero;

        cloudParticles = cloudObj.AddComponent<ParticleSystem>();
        cloudParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = cloudParticles.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.9f, 0.85f, 0.7f, 0.8f),
            new Color(1f, 1f, 0.9f, 0.6f)
        );
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.2f;

        var emission = cloudParticles.emission;
        emission.rateOverTime = 80f;

        var shape = cloudParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.6f;

        var colorOverLifetime = cloudParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.9f, 0.85f, 0.7f), 0f),
                new GradientColorKey(new Color(1f, 1f, 0.9f), 0.5f),
                new GradientColorKey(new Color(0.8f, 0.75f, 0.6f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.7f, 0.2f),
                new GradientAlphaKey(0.7f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = cloudParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var velocityOverLifetime = cloudParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.orbitalY = 5f;
        velocityOverLifetime.radial = -1f;

        // Use default particle material
        var renderer = cloudObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", new Color(0.9f, 0.85f, 0.7f, 0.6f));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private void CreateSparkleParticles()
    {
        // Sparkle/star particles
        GameObject sparkleObj = new GameObject("Sparkles");
        sparkleObj.transform.SetParent(transform, false);
        sparkleObj.transform.localPosition = Vector3.zero;

        sparkleParticles = sparkleObj.AddComponent<ParticleSystem>();
        sparkleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = sparkleParticles.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.5f, 1f),
            new Color(1f, 1f, 1f, 1f)
        );
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = sparkleParticles.emission;
        emission.rateOverTime = 40f;

        var shape = sparkleParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.8f;

        var colorOverLifetime = sparkleParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.5f), 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var velocityOverLifetime = sparkleParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.orbitalY = 8f;

        var renderer = sparkleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", new Color(1f, 0.95f, 0.5f, 1f));
        renderer.material.SetFloat("_Mode", 1f); // Additive
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private void CreateDustRingParticles()
    {
        // Dust ring at the feet
        GameObject dustObj = new GameObject("DustRing");
        dustObj.transform.SetParent(transform, false);
        dustObj.transform.localPosition = Vector3.down * 1f;

        dustRingParticles = dustObj.AddComponent<ParticleSystem>();
        dustRingParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = dustRingParticles.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.7f, 0.65f, 0.5f, 0.5f),
            new Color(0.85f, 0.8f, 0.65f, 0.4f)
        );
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = dustRingParticles.emission;
        emission.rateOverTime = 25f;

        var shape = dustRingParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.8f;
        shape.arc = 360f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

        var colorOverLifetime = dustRingParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.7f, 0.65f, 0.5f), 0f),
                new GradientColorKey(new Color(0.85f, 0.8f, 0.65f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.5f, 0.2f),
                new GradientAlphaKey(0.3f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", new Color(0.7f, 0.65f, 0.5f, 0.5f));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
