using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSimulation : MonoBehaviour
{
    [SerializeField] public ComputeShader _simulationShader;
    [SerializeField] public int _resolution = 1024;
    [SerializeField] public float _viscosity;
    [SerializeField, Range(1, 20)] public int _diffuseIterations;
    [SerializeField, Range(1, 80)] public int _pressureIterations;
    [SerializeField, Range(0, 1)] public float _decay;
    [SerializeField, Range(0, 50)] public float _velocityScale;

    [SerializeField] Material _debugMaterial;

    // r, g are velocity, and b is "concentration" 
    private RenderTexture _fluidA;
    private RenderTexture _fluidB;

    private RenderTexture _divergence;

    private RenderTexture _pressureA;
    private RenderTexture _pressureB;

    private Dictionary<string, int> _kernels = new Dictionary<string, int>();
    private string[] _kernelNames = new string[] {
        "Test",
        "Clear",
        "Advection",
        "Force",
        "Diffuse",
        "Divergence",
        "ProjectField",
        "Pressure",
        "BoundaryPressure",
        "BoundaryVelocity"
    };

    private int _groupX;
    private int _groupY;

    RenderTexture CreateTexture(int resolution){
        var texture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
        texture.enableRandomWrite = true;
        texture.Create();

        return texture;
    }

    void Start()
    {
        _fluidA = CreateTexture(_resolution);
        _fluidB = CreateTexture(_resolution);
        _divergence = CreateTexture(_resolution);
        _pressureA = CreateTexture(_resolution);
        _pressureB = CreateTexture(_resolution);

        foreach(string name in _kernelNames) {
            _kernels[name] = _simulationShader.FindKernel(name);
        }

        _groupX = Mathf.CeilToInt(_resolution / 8);
        _groupY = Mathf.CeilToInt(_resolution / 8);
    }

    void Destroy()
    {
        _fluidA.Release();
        _fluidB.Release();
        _divergence.Release();
        _pressureA.Release();
        _pressureB.Release();
    }

    Vector3 _lastMousePos;

    void DispatchKernel(string name, 
        RenderTexture sourceFluid = null, 
        RenderTexture destinationFluid = null,
        RenderTexture sourceDivergence = null,
        RenderTexture destinationDivergence = null, 
        RenderTexture sourcePressure = null, 
        RenderTexture destinationPressure = null) {

        if (sourceFluid != null)
        {
            _simulationShader.SetTexture(_kernels[name], "_SourceFluid", sourceFluid);
        }

        if (destinationFluid != null)
        {
            _simulationShader.SetTexture(_kernels[name], "_DestinationFluid", destinationFluid);
        }

        if (sourceDivergence != null)
        {
            _simulationShader.SetTexture(_kernels[name], "_SourceDivergence", sourceDivergence);
        }

        if (destinationDivergence != null)
        {
            _simulationShader.SetTexture(_kernels[name], "_DestinationDivergence", destinationDivergence);
        }

        if (sourcePressure != null)
        {
            _simulationShader.SetTexture(_kernels[name], "_SourcePressure", sourcePressure);
        }
        
        if (destinationPressure != null)
        {
            _simulationShader.SetTexture(_kernels[name], "_DestinationPressure", destinationPressure);
        }

        _simulationShader.Dispatch(_kernels[name], _groupX, _groupY, 1);
    }

    void Update() {
        // dx is pixel width
        float dx = 1 ;
        float dt = Time.deltaTime;
        float v = _viscosity;

        _simulationShader.SetFloat("_resolution", _resolution);
        _simulationShader.SetFloat("_viscosity", _viscosity);
        _simulationShader.SetFloat("_dt", dt);
        _simulationShader.SetFloat("_decay", _decay);

        _simulationShader.SetFloat("_halfrdx", 0.5f * (1.0f / dx));
        _simulationShader.SetFloat("_rdx", (1.0f / dx) );
        
        if (Input.GetMouseButton(0)) {
            _simulationShader.SetVector("_mousePos", Input.mousePosition);
            _simulationShader.SetVector("_mouseVelocity", _velocityScale * (Input.mousePosition - _lastMousePos));

            DispatchKernel("Force", destinationFluid: _fluidA);
        }

        _lastMousePos = Vector3.Lerp(_lastMousePos, Input.mousePosition, 0.5f);

        DispatchKernel("BoundaryVelocity", sourceFluid: _fluidA, destinationFluid: _fluidA);
        DispatchKernel("Advection", sourceFluid: _fluidA, destinationFluid: _fluidB);

        for(int i = 0; i < _diffuseIterations; i ++) {
            DispatchKernel("Diffuse", sourceFluid: _fluidB, destinationFluid: _fluidA);
            DispatchKernel("Diffuse", sourceFluid: _fluidA, destinationFluid: _fluidB);
        }

        DispatchKernel("Divergence", sourceFluid: _fluidB, destinationDivergence: _divergence);
        DispatchKernel("Clear", destinationFluid: _pressureA);

        for(int i = 0; i < _pressureIterations; i ++) {
            DispatchKernel("Pressure", sourceDivergence: _divergence, sourcePressure: _pressureA, destinationPressure: _pressureB);
            DispatchKernel("Pressure", sourceDivergence: _divergence, sourcePressure: _pressureB, destinationPressure: _pressureA);
        }

        DispatchKernel("BoundaryPressure", sourcePressure: _pressureA, destinationPressure: _pressureA);
        DispatchKernel("ProjectField", sourceFluid: _fluidB, destinationFluid: _fluidA, sourcePressure: _pressureA);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) 
    {
        // Graphics.Blit(_fluidB, _fluidA);
        _debugMaterial.SetTexture("_divergence", _divergence);
        _debugMaterial.SetTexture("_fluid", _fluidA);
        _debugMaterial.SetTexture("_pressure", _pressureA);

        Graphics.Blit(null, destination, _debugMaterial);
    }
}
