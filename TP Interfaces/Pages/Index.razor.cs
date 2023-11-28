using System;
using System.IO.Ports;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using TP_Interfaces.Data;
using Microsoft.JSInterop;
using System.Globalization;
using System.Speech.Recognition;
using MudBlazor;

namespace TP_Interfaces.Pages;

public partial class Index : ComponentBase
{
    [Inject] private IJSRuntime JSRuntime { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; } 

    SerialPort port = new SerialPort("COM4", 9600);
    private float percentage = 0;
    private int maxConnectRetries = 20;
    private bool isIngredient1Served = false, isIngredient2Served = false;

    SpeechRecognitionEngine talk = new SpeechRecognitionEngine(new CultureInfo("es-ES"));
    private static List<String> FernetWords = new() { "fernet", "con", "coca", "carnet", "cock", "cern", "sede", "gobierno", "a un" };
    private static List<String> GinTonicWords = new() { "gin", "and", "tonic", "china", "China", "Tony", "tónica", "sonic", "chile", "antoni", "antonio", "anthony", "siendo", "quien", "si", "el", "actor", "y" };
    private static List<String> DestornilladorWords = new() { "vodka", "des", "de", "destornillador" };

    Bebida FernetConCoca, Destornillador, GinTonic, selectedBeverage;

    enum Step { ConnectingToPort, SelectingBeverage, DevicePositioning, ServingBeverage, Finished }
    Step currentStep = Step.ConnectingToPort;

    enum RecordingStep { Start, Listening, Detected, Error } 
    RecordingStep recordingStep = RecordingStep.Start;

    protected override void OnInitialized()
    {
        port.RtsEnable = true;
        port.DtrEnable = true;

        talk.SetInputToDefaultAudioDevice();
        Grammar grammar = new DictationGrammar();
        talk.LoadGrammar(grammar);
        talk.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(SpeechRecognized);
        talk.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(SpeechHypothesized);

        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;
        Snackbar.Configuration.VisibleStateDuration = 2000;
        Snackbar.Configuration.RequireInteraction = false;
        Snackbar.Configuration.ShowCloseIcon = false;

        FernetConCoca = Bebida.FernetConCoca();
        Destornillador = Bebida.Destornillador();
        GinTonic = Bebida.GinAndTonic();
    }
    string text;
    private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        text = e.Result.Text.ToLower();

        if (CheckLibrary(text, FernetWords))
            SelectBeverage(FernetConCoca);
        else if (CheckLibrary(text, GinTonicWords))
            SelectBeverage(GinTonic);
        else if (CheckLibrary(text, DestornilladorWords))
            SelectBeverage(Destornillador);
        else
        {
            recordingStep = RecordingStep.Error;
            Snackbar.Clear();
            Snackbar.Add("Error al grabar. Intentá de nuevo", Severity.Warning);
        }

        StateHasChanged();
    }
    private void SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
    {
        recordingStep = RecordingStep.Detected; 
        StateHasChanged();
    }

    private bool CheckLibrary(string value, List<String> list) => list.Any(x => value.Contains(x));

    private async Task OpenPort()
    {
        while (maxConnectRetries > 0) 
        {
            try
            {
                port.Open();
                if (port.IsOpen)
                {
                    currentStep = Step.SelectingBeverage;
                    break;
                }
            }
            catch
            {
                if (port.IsOpen)
                    port.Close();
                await Task.Delay(250);
                maxConnectRetries--;
                StateHasChanged();
            }
        }
        maxConnectRetries = 20;
    }

    private void GoToStep(Step step)=> currentStep = step;

    private async Task Record()
    {
        recordingStep = RecordingStep.Listening;
        await Task.Delay(1);
        StateHasChanged();
        talk.Recognize();
    }

    private void SelectBeverage(Bebida beverage)
    {
        selectedBeverage = beverage;
        currentStep = Step.DevicePositioning;
    }

    private async Task ReadPort()
    {
        currentStep = Step.ServingBeverage;
        await Task.Delay(1);
        StateHasChanged();
        float[] measurements = new float[5];
        int selectedMeasure = 0;

        while (port.IsOpen) 
        {
            float.TryParse(port.ReadLine(), out measurements[selectedMeasure]);

            percentage = (measurements[0] + measurements[1] + measurements[2] + measurements[3] + measurements[4]) / 5;
            for (int i = 0; i < 4; i++)
            {
                if (measurements[i] < percentage - 5 || measurements[i] > percentage + 5)
                {
                    measurements[i] = percentage;
                    percentage = (measurements[0] + measurements[1] + measurements[2] + measurements[3] + measurements[4]) / 5;
                }
            }

            if (percentage > selectedBeverage.Ingredientes[0].Proportion)
            {
                if(!isIngredient1Served)
                    await JSRuntime.InvokeVoidAsync("Vibrate");
                isIngredient1Served = true;
            }

            if (percentage > selectedBeverage.Ingredientes[0].Proportion + selectedBeverage.Ingredientes[1].Proportion)
            {
                if (!isIngredient2Served)
                    await JSRuntime.InvokeVoidAsync("Vibrate");
                isIngredient2Served = true;
                currentStep = Step.Finished;
                break;
            }

            selectedMeasure = selectedMeasure >= 4 ? 0 : selectedMeasure + 1;

            await Task.Delay(1);
            StateHasChanged();
        }
    }

    //No lo voy a usar porque hay problemas de sincronización con el puerto serie al medir nuevamente
    private void Refresh()
    {
        isIngredient1Served = false;
        isIngredient2Served = false;
        percentage = 0;
        selectedBeverage = null;
        currentStep = Step.SelectingBeverage;
    }
}

    

