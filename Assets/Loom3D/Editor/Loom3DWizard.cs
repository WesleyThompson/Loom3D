using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Loom3D
{

    public class Loom3DWizard : ScriptableWizard
    {
        public Texture2D clothTexture;

        [Header("Mesh Generation Settings")]
        [Range(1, 256)]
        public int pixelsPerQuad = 32;
        [Tooltip("How big, in world-space units, each segment is")]
        public float quadSize = 0.1f;
        public bool generateMaterial = true;

        #region Wizard Menu Stuff
        private const string menuFolderName = "Loom3D/";
        private const string menuName = "Generate Cloth From Texture";
        private const string buttonName = "Generate Cloth";
        private const string noTextureHelpString = "Missing texture";
        #endregion

        #region Wizard Methods
        [MenuItem("Assets/" + menuFolderName + menuName)]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<Loom3DWizard>(menuName, buttonName);
        }

        //OnValidate for wizards. This is called when the wizard is opened 
        //or whenever the user changes something in the wizard.
        private void OnWizardUpdate()
        {
            //Is clothTexture assigned
            if (!clothTexture)
            {
                errorString = noTextureHelpString;
                isValid = false;
            }
            else
            {
                errorString = "";
                isValid = true;
            }
            
        }

        private void OnWizardCreate()
        {
            Loom3D.CreateMeshFromTexture(clothTexture, pixelsPerQuad, quadSize);

            if(generateMaterial)
            {
                Loom3D.CreateAndSaveMaterial(clothTexture);
            }
        }
        #endregion


    }
}