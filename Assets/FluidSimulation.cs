using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSimulation : MonoBehaviour
{
    [SerializeField] public ComputeShader _simulationShader;
    [SerializeField] public int _resolution = 1024;
    [SerializeField] public float _viscosity;
    [SerializeField, Range(1, 20)] public int _diffuseIterations;

    // r, g are velocity, and b is "concentration" 
    private RenderTexture _fluidA;
    private RenderTexture _fluidB;

    private RenderTexture _divergenceA;
    private RenderTexture _divergenceB;

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

        _divergenceA = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _divergenceA.enableRandomWrite = true;
        _divergenceA.Create();

        _divergenceB = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _divergenceB.enableRandomWrite = true;
        _divergenceB.Create();

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
        _divergenceA.Release();
        _divergenceB.Release();
        _pressureA.Release();
        _pressureB.Release();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) 
    {
        _simulationShader.SetFloat("_resolution", _resolution);
        _simulationShader.SetFloat("_viscosity", _viscosity);
        _simulationShader.SetFloat("_dt", Time.deltaTime);
        
        if ( Time.time < 1) {
            _simulationShader.SetTexture(_kernels["Test"], "_DestinationFluid", _fluidA);
            _simulationShader.Dispatch(_kernels["Test"], _groupX, _groupY, 1);
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

        // Graphics.Blit(_fluidB, _fluidA);
        Graphics.Blit(_fluidA, destination);
    }
}
