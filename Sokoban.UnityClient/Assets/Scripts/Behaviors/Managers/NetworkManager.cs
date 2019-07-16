using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using static WarehouseManager;

public class NetworkManager : BaseBehavior
{
    private WarehouseManager.MetaData warehouseMetaData { get; set; }

    private SokobanNetwork networkHandler { get; set; }

    protected override void Start()
    {
        base.Start();
        // this.warehouseMetaData = this.File.Get<WarehouseManager.MetaData>();
        // this.networkHandler = new SokobanNetwork(this, warehouseMetaData.userData);

        // if (!StringExtensions.IsValid(this.warehouseMetaData.userData?.authToken))
        // {
        //     var response = await this.GetIdentity(this.warehouseMetaData.GetIdentifier());
        //     if (response.IsSuccess)
        //     {
        //         this.warehouseMetaData.userData = response.result;
        //         Debug.Log($"Success! Authed as {response.result.generatedName}");
        //     }
        // }
    }

    async Task<SokobanRequestHandler<WarehouseManager.MetaData.UserData>> GetIdentity(string deviceIdentifier)
    {
        return await this.networkHandler.Request<WarehouseManager.MetaData.UserData>
        (
            method: UnityWebRequest.kHttpVerbPUT,
            endPoint: "identities",
            body: new { deviceIdentifier }
        )
        .send();
    }
}

public class SokobanRequestHandler<T>
{
    public enum RequestStatusType
    {
        PendingSend,
        AwaitingResponse,
        Failure,
        Success
    }

    public UnityWebRequest request { get; set; }
    public T result { get; set; }
    public DateTime startedAt { get; private set; }
    public DateTime endedAt { get; private set; }
    public RequestStatusType status { get; private set; } = RequestStatusType.PendingSend;

    private Action<T> onSuccessHandler { get; set; }
    private Action<string> onFailureHandler { get; set; }

    #region properties
    public TimeSpan Elapsed
    {
        get
        {
            return this.endedAt - this.startedAt;
        }
    }
    public string ElapsedString
    {
        get
        {
            var elapsed = this.Elapsed.TotalSeconds.ToString("#.##");
            return $"{elapsed} sec.";
        }
    }
    public bool IsSuccess
    {
        get
        {
            return this.status == RequestStatusType.Success;
        }
    }
    #endregion

    public SokobanRequestHandler(UnityWebRequest request, Action<T> onSuccessHandler = null, Action<string> onFailureHandler = null)
    {
        this.request = request;
        this.onSuccessHandler = onSuccessHandler;
        this.onFailureHandler = onFailureHandler;
    }

    public async Task<SokobanRequestHandler<T>> send()
    {
        try
        {
            this.startedAt = DateTime.UtcNow;
            this.status = RequestStatusType.AwaitingResponse;
            await request.SendWebRequest();
            this.endedAt = DateTime.UtcNow;

            if (request.isNetworkError || request.isHttpError)
            {
                this.onFailure($"({request.responseCode}) {request.error}");
            }
            else
            {
                var responseString = request.downloadHandler.text;
                if (!typeof(T).IsAssignableFrom(typeof(string)))
                {
                    this.result = JsonConvert.DeserializeObject<T>(responseString);
                }
                this.onSuccess();
            }
        }
        catch (Exception e)
        {
            if (this.endedAt < this.startedAt)
            {
                this.endedAt = DateTime.UtcNow;
            }
            this.onFailure(e.Message);
        }

        return this;
    }

    protected virtual void onSuccess()
    {
        this.status = RequestStatusType.Success;
        Debug.Log($"Sokoban Network: Response success after {this.ElapsedString} - {request.downloadHandler.text}");
        this.onSuccessHandler?.Invoke(this.result);
    }

    protected virtual void onFailure(string errorMessage)
    {
        this.status = RequestStatusType.Failure;
        Debug.Log($"Sokoban Network: Request failure after {this.ElapsedString} - {errorMessage}");
        this.onFailureHandler?.Invoke(errorMessage);
    }
}

public class SokobanNetwork
{
    private static readonly string baseUrl = "http://localhost:5000/api/";
    private static readonly string headerToken = "x-auth-token";
    private static readonly string headerIdentity = "x-auth-identity";

    public MetaData.UserData userData { get; set; }

    private BaseBehavior behavior { get; set; }

    public SokobanNetwork(BaseBehavior behavior, MetaData.UserData userData)
    {
        this.behavior = behavior;
        this.userData = userData;
    }

    public SokobanRequestHandler<T> Request<T>(string method, string endPoint, object body) =>
        this.Request<T>(method, endPoint, JsonConvert.SerializeObject(body));
    public SokobanRequestHandler<T> Request<T>(string method, string endPoint, string body = null)
    {
        var fullUrl = $"{baseUrl}{endPoint}";
        UnityWebRequest request = null;
        switch (method)
        {
            case UnityWebRequest.kHttpVerbGET:
                request = UnityWebRequest.Get(fullUrl);
                break;
            case UnityWebRequest.kHttpVerbPOST:
                request = UnityWebRequest.Post(fullUrl, body);
                break;
            case UnityWebRequest.kHttpVerbPUT:
                request = UnityWebRequest.Put(fullUrl, body);
                break;
        }
        if (body.IsValid())
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.uploadHandler.contentType = "application/json";
        }
        Debug.Log($"Sokoban Network: Request ({method}) - {fullUrl}");
        this.addHeaders(request);
        return new SokobanRequestHandler<T>(request);
    }

    private void addHeaders(UnityWebRequest request)
    {
        if (this.userData?.authToken.IsValid() == true)
        {
            request.SetRequestHeader(headerToken, this.userData.authToken);
        }
        if (this.userData?.id.IsValid() == true)
        {
            request.SetRequestHeader(headerIdentity, this.userData.id);
        }
    }
}

