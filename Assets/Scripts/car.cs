using System;
using UnityEngine;

[Serializable]
public class WheelProperties
{
    public int WheelState = 1; // 1 = steerable (ön), 0 = free (arka)
    public bool isDriveWheel = true;
    [HideInInspector] public float biDirectional = 0;
    public Vector3 localPosition;
    public float turnAngle = 30f;

    [HideInInspector] public float lastSuspensionLength = 0.0f;
    [HideInInspector] public Vector3 localSlipDirection;
    [HideInInspector] public Vector3 worldSlipDirection;
    [HideInInspector] public Vector3 suspensionForceDirection;
    [HideInInspector] public Vector3 wheelWorldPosition;
    [HideInInspector] public float wheelCircumference;
    [HideInInspector] public float torque = 0.0f;
    [HideInInspector] public Rigidbody parentRigidbody;
    [HideInInspector] public GameObject wheelObject;
    [HideInInspector] public float hitPointForce;
    [HideInInspector] public Vector3 localVelocity;
}

public class car : MonoBehaviour
{
    [Header("Wheel Setup")]
    public GameObject wheelPrefab;
    public WheelProperties[] wheels;

    [Header("Suspension")]
    public float suspensionStiffness = 1.5f;
    public float dampingMultiplier = 1.0f;

    [Header("Otomatik Vites")]
    public float[] gearRatios = { 0f, 3.5f, 2.5f, 1.8f, 1.3f, 1.0f, 0.8f };
    public float reverseGearRatio = 3.0f;

    // ============ INTERNAL — Inspector'da gösterilmez ============
    private float wheelSize;
    private float maxTorque;
    private float frontGrip;
    private float rearGrip;
    private float maxGripFront;
    private float maxGripRear;
    private float rollingFriction;
    private float maxSpeed;
    private float throttleSmoothing;
    private float brakeSmoothSpeed;
    private float engineBrakeForce;
    private float airDrag;

    // Vites geçiş hızları (m/s) — her 10 km/h
    private float[] shiftUpSpeeds = { 0f, 2.78f, 5.56f, 8.33f, 11.11f, 13.89f, 999f };

    // State
    private int currentGear = 1;
    private float engineRPM = 800f;
    private bool isReverse = false;
    private float smoothGas = 0f;
    private float smoothBrake = 0f;
    private bool forwards = false;

    // Suspension auto
    private float autoSpringK;
    private float autoDampK;
    private float autoClamp;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();

        // === SABİT DEĞERLER — Inspector cache'ini geçersiz kılar ===
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 3f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Kütle merkezi: hafif aşağıda ama çok değil — takla atabilmesi için
        rb.centerOfMass = new Vector3(0f, -0.05f, 0.05f);

        // Süspansiyon sönümleme
        dampingMultiplier = 1.8f;

        wheelSize = 0.53f;
        maxTorque = 6f;
        frontGrip = 14f;        // ön tekerlekler daha fazla tutar
        rearGrip = 8f;          // arka tekerlekler daha az tutar → savrulma
        maxGripFront = 16f;
        maxGripRear = 10f;
        rollingFriction = 0.03f; // düşük → gaz bırakınca uzun kayma
        maxSpeed = 16.67f;       // 60 km/h
        throttleSmoothing = 0.5f;
        brakeSmoothSpeed = 1.5f;
        engineBrakeForce = 0.15f; // düşük motor freni → uzun kayma
        airDrag = 0.008f;        // düşük hava direnci → momentum korunur

        // === OTOMATİK SÜSPANSİYON ===
        if (wheels != null && wheels.Length > 0)
        {
            float gravity = Mathf.Abs(Physics.gravity.y);
            float weightPerWheel = rb.mass * gravity / wheels.Length;
            float rayLen = wheelSize * 2f;
            float restRatio = 0.3f;

            autoSpringK = (weightPerWheel / (restRatio * rayLen)) * suspensionStiffness;
            autoDampK = 2f * Mathf.Sqrt(autoSpringK * rb.mass / wheels.Length) * 0.6f * dampingMultiplier;
            autoClamp = weightPerWheel * 4f;

            for (int i = 0; i < wheels.Length; i++)
            {
                WheelProperties w = wheels[i];
                Vector3 relPos = transform.InverseTransformPoint(transform.TransformPoint(w.localPosition));
                w.localPosition = relPos;

                // === OTOMATİK TEKERLEK AYARI ===
                // Z pozitif = ön tekerlek (direksiyon), Z negatif = arka tekerlek
                if (w.localPosition.z >= 0f)
                {
                    w.WheelState = 1;
                    w.turnAngle = 30f;
                }
                else
                {
                    w.WheelState = 0;
                    w.turnAngle = 0f;
                }

                // Tüm tekerlekler çekiş alır (AWD)
                w.isDriveWheel = true;

                w.wheelObject = Instantiate(wheelPrefab, transform);
                w.wheelObject.transform.localPosition = w.localPosition;
                w.wheelObject.transform.eulerAngles = transform.eulerAngles;
                w.wheelObject.transform.localScale = 2f * new Vector3(wheelSize, wheelSize, wheelSize);
                w.wheelCircumference = 2f * Mathf.PI * wheelSize;
                w.parentRigidbody = rb;
            }
        }
    }

    void FixedUpdate()
    {
        if (wheels == null || wheels.Length == 0) return;

        // === GİRİŞLER ===
        float rawGas = Mathf.Max(0f, Input.GetAxis("Vertical"));   // W = pozitif
        float rawBrake = Mathf.Max(0f, -Input.GetAxis("Vertical")); // S = negatif
        float steer = Input.GetAxis("Horizontal");                   // A/D

        // Yumuşak gaz — kademeli hızlanma
        smoothGas = Mathf.MoveTowards(smoothGas, rawGas, Time.fixedDeltaTime * throttleSmoothing);

        // Yumuşak fren — kademeli yavaşlama
        smoothBrake = Mathf.MoveTowards(smoothBrake, rawBrake, Time.fixedDeltaTime * brakeSmoothSpeed);

        float speed = rb.linearVelocity.magnitude;
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // Hız limiti — ileri max 60 km/h, geri max 20 km/h
        float reverseMaxSpeed = 5.56f; // 20 km/h
        if (!isReverse && speed > maxSpeed) smoothGas = 0f;
        if (isReverse && speed > reverseMaxSpeed) smoothBrake = 0f;

        // === HAVA DİRENCİ — düşük, momentum korunur ===
        if (speed > 0.1f)
            rb.AddForce(-airDrag * speed * rb.linearVelocity, ForceMode.Force);

        // === GERİ VİTES MANTIĞI ===
        if (smoothBrake > 0.1f && speed < 0.3f && forwardSpeed <= 0.1f)
            isReverse = true;
        if (smoothGas > 0.1f)
            isReverse = false;

        // === OTOMATİK VİTES ===
        if (!isReverse)
        {
            if (currentGear < gearRatios.Length - 1 && speed > shiftUpSpeeds[currentGear])
                currentGear++;
            if (currentGear > 1 && speed < shiftUpSpeeds[currentGear - 1] - 1f)
                currentGear--;
        }

        // === RPM & TORK ===
        float gearRatio = isReverse ? reverseGearRatio : gearRatios[currentGear];
        engineRPM = Mathf.Clamp(800f + speed * gearRatio * 600f, 800f, 7000f);
        float rpmN = (engineRPM - 800f) / 6200f;
        float torqueCurve = Mathf.Clamp(1f - 0.6f * rpmN * rpmN, 0.1f, 1f);
        float effectiveTorque = maxTorque * gearRatio * torqueCurve;

        // Sürüş kuvveti
        float driveInput = 0f;
        if (isReverse && smoothBrake > 0.01f)
            driveInput = -smoothBrake;
        else if (!isReverse && smoothGas > 0.01f)
            driveInput = smoothGas;

        // === TEKERLEK FİZİĞİ ===
        foreach (var wheel in wheels)
        {
            if (!wheel.wheelObject) continue;

            Transform wheelObj = wheel.wheelObject.transform;
            Transform wheelVisual = wheelObj.GetChild(0);

            // Ön/arka tekerlek grip değerleri
            bool isFront = (wheel.WheelState == 1);
            float gripForce = isFront ? frontGrip : rearGrip;
            float gripMax = isFront ? maxGripFront : maxGripRear;

            // --- DİREKSİYON (sadece ön) ---
            if (isFront)
            {
                float speedFactor = Mathf.Clamp01(1f - speed / (maxSpeed * 1.2f));
                float effectiveAngle = wheel.turnAngle * steer * (0.3f + 0.7f * speedFactor);
                Quaternion targetRot = Quaternion.Euler(0, effectiveAngle, 0);
                wheelObj.localRotation = Quaternion.Lerp(
                    wheelObj.localRotation, targetRot, Time.fixedDeltaTime * 100f);
            }
            // Arka tekerlekler: SABİT — hiçbir rotasyon değişikliği yapılmaz

            // Pozisyon ve hız
            wheel.wheelWorldPosition = transform.TransformPoint(wheel.localPosition);
            Vector3 velAtWheel = rb.GetPointVelocity(wheel.wheelWorldPosition);
            wheel.localVelocity = wheelObj.InverseTransformDirection(velAtWheel);

            // --- MOTOR ---
            float wheelTorque = 0f;
            if (wheel.isDriveWheel)
            {
                wheelTorque = driveInput * effectiveTorque;

                // Motor freni
                if (Mathf.Abs(driveInput) < 0.01f && !isReverse && Mathf.Abs(wheel.localVelocity.z) > 0.1f)
                    wheelTorque = -engineBrakeForce * wheel.localVelocity.z;
            }

            // Fren — ileri giderken S (kademeli)
            if (!isReverse && smoothBrake > 0.01f && forwardSpeed > 0.3f)
                wheelTorque = -maxTorque * 2f * smoothBrake;

            wheel.torque = wheelTorque;

            // Yuvarlanma sürtünmesi
            float rollFric = -rollingFriction * wheel.localVelocity.z;

            // --- YANAL GRİP ---
            float latFric = Mathf.Clamp(-gripForce * wheel.localVelocity.x, -gripMax, gripMax);
            float engForce = wheel.torque / wheelSize;

            Vector3 totalLocal = new Vector3(latFric, 0f, rollFric + engForce);
            wheel.localSlipDirection = totalLocal;
            Vector3 totalWorld = wheelObj.TransformDirection(totalLocal);
            forwards = (wheel.localVelocity.z > 0f);

            // === SÜSPANSİYON ===
            float rayLen = wheelSize * 2f;
            RaycastHit hit;
            Debug.DrawRay(wheel.wheelWorldPosition, -transform.up * rayLen, Color.green);

            if (Physics.Raycast(wheel.wheelWorldPosition, -transform.up, out hit, rayLen))
            {
                float compression = rayLen - hit.distance;
                float dampVel = wheel.lastSuspensionLength - hit.distance;
                float spring = Mathf.Clamp(compression * autoSpringK + dampVel * autoDampK, 0f, autoClamp);

                Vector3 springDir = hit.normal * spring;
                wheel.suspensionForceDirection = springDir;
                rb.AddForceAtPosition(springDir + totalWorld, hit.point);

                wheelObj.position = hit.point + transform.up * wheelSize;
                wheel.lastSuspensionLength = hit.distance;
            }
            else
            {
                wheelObj.position = wheel.wheelWorldPosition - transform.up * wheelSize;
                wheel.lastSuspensionLength = rayLen;
            }

            // Görsel dönüş
            Vector3 fwd;
            if (isFront)
                fwd = wheelObj.InverseTransformDirection(rb.GetPointVelocity(wheel.wheelWorldPosition));
            else
                fwd = transform.InverseTransformDirection(rb.GetPointVelocity(wheel.wheelWorldPosition));

            float rotSpeed = fwd.z * 360f / wheel.wheelCircumference;
            wheelVisual.Rotate(Vector3.up, -rotSpeed * Time.fixedDeltaTime, Space.Self);
        }
    }

    // === HUD ===
    void OnGUI()
    {
        float speedKmh = rb ? rb.linearVelocity.magnitude * 3.6f : 0f;
        string gearText = isReverse ? "R" : currentGear.ToString();

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        GUI.Box(new Rect(10, 10, 250, 140), "");
        GUI.Label(new Rect(20, 20, 400, 40), "Hiz: " + speedKmh.ToString("F0") + " km/h", style);
        GUI.Label(new Rect(20, 55, 400, 40), "Vites: " + gearText, style);
        GUI.Label(new Rect(20, 90, 400, 40), "RPM: " + engineRPM.ToString("F0"), style);
    }
}