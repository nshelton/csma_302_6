using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSimulation : MonoBehaviour
{
    [SerializeField] public ComputeShader _simulationShader;
    [SerializeField] public int _resolution = 1024;
    [SerializeField] public float _viscosity;
    [SerializeField, Range(1, 20)] public int _diffuseIterations;
    [SerializeField, Range(1, 20)] public int _pressureIterations;

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
        "Pressure"
    };

    private int _groupX;
    private int _groupY;

    void Start()
    {
        _fluidA = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _fluidA.enableRandomWrite = true;
        _fluidA.Create();

        _fluidB = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _fluidB.enableRandomWrite = true;
        _fluidB.Create();

        _divergence = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _divergence.enableRandomWrite = true;
        _divergence.Create();

        _pressureA = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _pressureA.enableRandomWrite = true;
        _pressureA.Create();

        _pressureB = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _pressureB.enableRandomWrite = true;
        _pressureB.Create();

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

    void Update() {
        // dx is pixel width
        float dx = 1 ;
        float dt = Time.deltaTime;
        float v = _viscosity;

        _simulationShader.SetFloat("_resolution", _resolution);
        _simulationShader.SetFloat("_viscosity", _viscosity);
        _simulationShader.SetFloat("_dt", dt);

        _simulationShader.SetFloat("_alpha", (dx * dx) / (v * dt) );
        _simulationShader.SetFloat("_rBeta", 1/(4 + (dx * dx)/(v * dt)));
        _simulationShader.SetFloat("_halfrdx", 0.5f * (1.0f / dx) );
        _simulationShader.SetFloat("_rdx", (1.0f / dx) );
        
        // if ( Time.time < 0.1) {
        //     _simulationShader.SetTexture(_kernels["Test"], "_DestinationFluid", _fluidA);
        //     _simulationShader.Dispatch(_kernels["Test"], _groupX, _groupY, 1);
        // } 

        if (Input.GetMouseButtonDown(0)){

            _simulationShader.SetTexture(_kernels["Force"], "_DestinationFluid", _fluidA);
            _simulationShader.SetVector("_mousePos", Input.mousePosition);
            _simulationShader.SetVector("_mouseVelocity", (Input.mousePosition - _lastMousePos) / dt);
            _simulationShader.Dispatch(_kernels["Force"], _groupX, _groupY, 1);

            _lastMousePos = Input.mousePosition;
        }

        _simulationShader.SetTexture(_kernels["Advection"], "_SourceFluid", _fluidA);
        _simulationShader.SetTexture(_kernels["Advection"], "_DestinationFluid", _fluidB);
        _simulationShader.Dispatch(_kernels["Advection"], _groupX, _groupY, 1);

        for(int i = 0; i < _diffuseIterations; i ++) {
            _simulationShader.SetTexture(_kernels["Diffuse"], "_SourceFluid", _fluidB);
            _simulationShader.SetTexture(_kernels["Diffuse"], "_DestinationFluid", _fluidA);
            _simulationShader.Dispatch(_kernels["Diffuse"], _groupX, _groupY, 1);
            
            _simulationShader.SetTexture(_kernels["Diffuse"], "_SourceFluid", _fluidA);
            _simulationShader.SetTexture(_kernels["Diffuse"], "_DestinationFluid", _fluidB);
            _simulationShader.Dispatch(_kernels["Diffuse"], _groupX, _groupY, 1);
        }

        _simulationShader.SetTexture(_kernels["Divergence"], "_SourceFluid", _fluidB);
        _simulationShader.SetTexture(_kernels["Divergence"], "_DestinationDivergence", _divergence);
        _simulationShader.Dispatch(_kernels["Divergence"], _groupX, _groupY, 1);

        _simulationShader.SetTexture(_kernels["Clear"], "_DestinationFluid", _pressureA);
        _simulationShader.Dispatch(_kernels["Clear"], _groupX, _groupY, 1);

        for(int i = 0; i < _pressureIterations; i ++) {
            _simulationShader.SetTexture(_kernels["Pressure"], "_SourcePressure", _pressureA);
            _simulationShader.SetTexture(_kernels["Pressure"], "_SourceDivergence", _divergence);
            _simulationShader.SetTexture(_kernels["Pressure"], "_DestinationPressure", _pressureB);
            _simulationShader.Dispatch(_kernels["Pressure"], _groupX, _groupY, 1);
            
            _simulationShader.SetTexture(_kernels["Pressure"], "_SourcePressure", _pressureB);
            _simulationShader.SetTexture(_kernels["Pressure"], "_SourceDivergence", _divergence);
            _simulationShader.SetTexture(_kernels["Pressure"], "_DestinationPressure", _pressureA);
            _simulationShader.Dispatch(_kernels["Pressure"], _groupX, _groupY, 1);
        }

        _simulationShader.SetTexture(_kernels["ProjectField"], "_SourceFluid", _fluidB);
        _simulationShader.SetTexture(_kernels["ProjectField"], "_DestinationFluid", _fluidA);
        _simulationShader.SetTexture(_kernels["ProjectField"], "_SourcePressure", _pressureA);
        _simulationShader.Dispatch(_kernels["ProjectField"], _groupX, _groupY, 1);
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
