﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FNPlugin
{
    abstract class EngineECU2 : FNResourceSuppliableModule
    {
        [KSPField(isPersistant = true, guiActive = true,guiActiveEditor = true, guiName = "Fuel Config")]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedFuel = 0;

        bool hasMultipleConfigurations = false;


        // Persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk1 = 0.2f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk2 = 0.1f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk3 = 0.05f;

        // None Persistant 

        [KSPField(isPersistant = false)]
        public float maxThrust = 75;
        public float MaxThrust { get { return maxThrust * ActiveConfiguration.thrustMult; } }

        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded = 300;
        public float MaxThrustUpgraded { get { return maxThrustUpgraded * ActiveConfiguration.thrustMult; } }

        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded2 = 1200;
        public float MaxThrustUpgraded2 { get { return maxThrustUpgraded2 * ActiveConfiguration.thrustMult; } }

  

        [KSPField(isPersistant = false)]
        public float efficiency = 0.19f;
        [KSPField(isPersistant = false)]
        public float efficiencyUpgraded = 0.38f;
        [KSPField(isPersistant = false)]
        public float efficiencyUpgraded2 = 0.76f;

        [KSPField(isPersistant = false)]
        public bool isLoaded = false;


        // Use for SETI Mode

        [KSPField(isPersistant = false)]
        public float maxTemp = 2500;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;


        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thrust", guiUnits = " kN")]
        public float maximumThrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Current Throtle", guiFormat = "F2")]
        public float throttle;

        public ModuleEngines curEngineT;

        private FuelConfiguration activeConfiguration;
        

        public FuelConfiguration ActiveConfiguration
        {
            get
            {
                if (activeConfiguration == null) activeConfiguration = FuelConfigurations[selectedFuel];
                return activeConfiguration;
            }
        }

        private IList<FuelConfiguration> fuelConfigurations;

        public IList<FuelConfiguration> FuelConfigurations
        {
            get
            {
                if (fuelConfigurations == null)
                {
                    fuelConfigurations = part.FindModulesImplementing<FuelConfiguration>().OrderByDescending(f => f.maxThrust).ToList();
                        
                }
                return fuelConfigurations;
            }
        }
        private void InitializeFuelSelector()
        {
            Debug.Log("[KSP Interstellar] Setup Transmit Fuels Configurations for " + part.partInfo.title);

            var chooseField = Fields["selectedFuel"];
            var chooseOptionEditor = chooseField.uiControlEditor as UI_ChooseOption;
            var chooseOptionFlight = chooseField.uiControlFlight as UI_ChooseOption;

            chooseField.guiActive =  FuelConfigurations.Count > 1;

            var names = FuelConfigurations.Select(m => m.fuelConfigurationName).ToArray() ;

            chooseOptionEditor.options = names;
            chooseOptionFlight.options = names;

          //  UpdateFromGUI(chooseField, selectedFuel);

            // connect on change event
            chooseOptionEditor.onFieldChanged += UpdateEditorGUI;
            chooseOptionFlight.onFieldChanged += UpdateFlightGUI;
        }

        private void UpdateEditorGUI(BaseField field, object oldFieldValueObj)
        {
            
            UpdateFromGUI(field, oldFieldValueObj);
            UpdateFuel();

        }
         private void UpdateFlightGUI(BaseField field, object oldFieldValueObj)
        {
            
            UpdateFromGUI(field, oldFieldValueObj);
            UpdateFuel();

        }

        private void UpdateFuel()
        {
            Debug.Log("Update Fuel");
            
            ConfigNode akPropellants = new ConfigNode();

            int I = 0;
               
            while (I < ActiveConfiguration.Fuels.Length)
            {
                akPropellants.AddNode(LoadPropellant(ActiveConfiguration.Fuels[I], ActiveConfiguration.Ratios[I]));
                I++;
            }
            
            curEngineT.atmosphereCurve = activeConfiguration.atmosphereCurve;
            curEngineT.Load(akPropellants);

            vessel.ClearStaging();
            vessel.ResumeStaging();


        }
        private void UpdateResources()
        {
            int i = 0;
                while (i < ActiveConfiguration.Fuels.Length)
            {
                
            } 
        }

        private ConfigNode LoadPropellant(string akName, float akRatio)
        {
        //    Debug.Log("Name: "+ akName);
        //    Debug.Log("Ratio: "+ akRatio);
            Propellant akPropellant = new Propellant();
            ConfigNode PropellantNode = new ConfigNode().AddNode("PROPELLANT");
            PropellantNode.AddValue("name", akName);
            PropellantNode.AddValue("ratio", akRatio);
            PropellantNode.AddValue("DrawGauge", true);
            akPropellant.Load(PropellantNode);

            return PropellantNode;
        }


        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            Debug.Log("[KSP Interstellar] UpdateFromGUI is called with " + selectedFuel);

            if (!FuelConfigurations.Any())
            {
                Debug.Log("[KSP Interstellar] UpdateFromGUI no FuelConfigurations found");
                return;
            }

            if (isLoaded == false)
                LoadInitialConfiguration();
            else
            {
                if (selectedFuel < FuelConfigurations.Count)
                {
                    Debug.Log("[KSP Interstellar] UpdateFromGUI " + selectedFuel + " < orderedFuelGenerators.Count");
                    activeConfiguration = FuelConfigurations[selectedFuel];
                }
                else
                {
                    Debug.Log("[KSP Interstellar] UpdateFromGUI " + selectedFuel + " >= orderedFuelGenerators.Count");
                    selectedFuel = FuelConfigurations.Count - 1;
                    activeConfiguration = FuelConfigurations.Last();
                }
            }

            if (activeConfiguration == null)
            {
                Debug.Log("[KSP Interstellar] UpdateFromGUI no activeConfiguration found");
                return;
            }

        }
        private void LoadInitialConfiguration()
        {
            isLoaded = true;

         //   var currentmaxIsp = maxIsp != 0 ? maxIsp : 1;

            //Debug.Log("[KSP Interstellar] UpdateFromGUI initialize initial fuel configuration with maxIsp target " + currentmaxIsp);

            // find maxIsp closes to target maxIsp
            activeConfiguration = FuelConfigurations.FirstOrDefault();
            selectedFuel = 0;
       //     var lowestmaxIspDifference = Math.Abs(currentmaxIsp - activeConfiguration.maxIsp);
            if (FuelConfigurations.Count > 1)
            {
                hasMultipleConfigurations = true;
            }
        }

        public override void OnUpdate()
        {
            //    InitializeFuelSelector();
            
            base.OnUpdate();
        }
        public override void OnStart(StartState state)
        {
         //   Debug.Log("Engine OnStart");
            InitializeFuelSelector();
        //    UpdateFuel();
            base.OnStart(state);
        }
        public override void OnLoad(ConfigNode node)
        {
          //  InitializeFuelSelector();
          //  UpdateFuel();
            base.OnLoad(node);
        }

    }
    class FuelConfiguration : PartModule
    {
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fuel Configuration")]
        public string fuelConfigurationName = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fuels")]
        public string fuels = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ratios")]
        public string ratios= "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Amount")]
        public string amount = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Amount")]
        public string maxAmount = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max ISP")]
        public float maxIsp = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "ThrustMult")]
        public float thrustMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thrust")]
        public float maxThrust = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Atmopheric Curve")]
        public FloatCurve atmosphereCurve = new FloatCurve();
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ignore ISP")]
        public string ignoreForIsp ="";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ignore Thrust")]
        public string ignoreForThrustCurve="";

        private string[] akFuels = new string[0];
        private bool[] akIgnoreIsp = new bool[0];
        private bool[] akIgnoreThrust = new bool[0];
        private float[] akRatio = new float[0];
        private float[] akAmount = new float[0];
        private float[] akMaxAmount = new float[0];

        public string[] Fuels
        {
            get
            {
               if (akFuels.Length == 0) akFuels = Regex.Replace(fuels, " ", "").Split(',');
                //    Debug.Log("Fuels: "+ akString[0]);
                return akFuels;
            }
        }

        public float[] Ratios
        {
            get
            {
                if (akRatio.Length == 0) akRatio = StringToFloatArray(ratios);
              //  Debug.Log("Ratios: " + ratios);
              //  Debug.Log("First Ratio Float: "+akRatio[0]);
                return akRatio;
            }
        }
        public float[] Amount
        {
            get
            {
                if (akAmount.Length == 0) akAmount = StringToFloatArray(amount);
                return akAmount;
            }
        }
        public float[] MaxAmount
        {
            get
            {
                if (akMaxAmount.Length == 0) akMaxAmount = StringToFloatArray(maxAmount);
                return akMaxAmount;
            }
        }
        public bool[] IgnoreForIsp
        {
            get
            {
                if (ignoreForIsp == "") akIgnoreIsp = falseBoolArray();
                else if (akIgnoreIsp.Length == 0) akIgnoreIsp = StringToBoolArray(ignoreForIsp);
                return akIgnoreIsp;
            }

        }
        public bool[] IgnoreForThrust
        {
            get
            {
                if (ignoreForThrustCurve == "") akIgnoreThrust = falseBoolArray();
                else if (akIgnoreThrust.Length == 0) akIgnoreIsp = StringToBoolArray(ignoreForIsp);
                return akIgnoreThrust;
            }

        }
        private bool[] falseBoolArray()
        {
            List<bool> akBoolList = new List<bool>();
            int I = 0;
            while (I < akFuels.Length)
            {

                akBoolList.Add(false);
                I++;
            }
            return akBoolList.ToArray();
        }
        private float[] StringToFloatArray(string akString)
        {
            List<float> akFloat = new List<float>();
            string[] arString = Regex.Replace(akString, " ", "").Split(',');
            int I = 0;
            while (I < arString.Length)
            {
                akFloat.Add((float)Convert.ToDouble(arString[I]));
                I++;
            }
            return akFloat.ToArray();
        }
        private bool[] StringToBoolArray(string akString)
        {
            List<bool> akBool = new List<bool>();
            string[] arString = Regex.Replace(akString, " ", "").Split(',');
            int I = 0;
            while (I < arString.Length)
            {
                akBool.Add(Convert.ToBoolean(arString[I]));
                I++;
            }
            return akBool.ToArray();
        }

    }
}
