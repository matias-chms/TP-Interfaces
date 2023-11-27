using System;
using System.IO.Ports;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using TP_Interfaces.Data;
using Microsoft.JSInterop;
using System.Globalization;
using System.Speech.Recognition;

namespace TP_Interfaces.Pages;

public partial class Index : ComponentBase
{
    [Inject] private IJSRuntime JSRuntime { get; set; }

    SerialPort port = new SerialPort("COM4", 9600);
    private float percentage = 0;
    private int maxConnectRetries = 20;
    private bool reintentar = false;
    bool isIngredient1Served = false, isIngredient2Served = false;

    SpeechRecognitionEngine talk = new SpeechRecognitionEngine(new CultureInfo("es-ES"));
    private static List<String> Fernet = new() { "fernet", "con", "coca", "carnet", "cock", "cern" };
    private static List<String> Gin = new() { "gin", "and", "tonic", "china", "China", "Tony", "tónica", "sonic", "Chile", "Antoni", "Antonio", "Anthony", "siendo", "quien" };
    private static List<String> Vodka = new() { "vodka", "des", "de" };

    Bebida FernetConCoca, Destornillador, GinTonic, selectedBeverage;

    enum Step { ConnectingToPort,Recording, SelectingBeverage, DevicePositioning, ServingBeverage, Finished }
    Step currentStep = Step.ConnectingToPort;

    protected override void OnInitialized()
    {
        port.RtsEnable = true;
        port.DtrEnable = true;

        talk.SetInputToDefaultAudioDevice();
        Grammar grammar = new DictationGrammar();
        talk.LoadGrammar(grammar);
        talk.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(talk_SpeechRecognized);

        FernetConCoca = Bebida.FernetConCoca();
        Destornillador = Bebida.Destornillador();
        GinTonic = Bebida.GinAndTonic();
    }
    private void talk_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        string text = e.Result.Text;
        //test = e.Result.Text;
        if (isLibreria(text, Fernet))
            SelectBeverage(FernetConCoca);
        else if (isLibreria(text, Gin))
            SelectBeverage(GinTonic);
        else if (isLibreria(text, Vodka))
            SelectBeverage(Destornillador);
        else {
            reintentar = true;
        }
        StateHasChanged();
    }
    private bool isLibreria(string value, List<String> list)
    {
        return list.Any(x => value.Contains(x));
    }
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

    private void Record()
    {
        currentStep = Step.Recording;
        StateHasChanged();
        talk.Recognize(TimeSpan.FromSeconds(5));

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

    

