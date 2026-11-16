input("*.fbx")
{
	feature("source", "project");
	feature("menu", "0.Asset Processors/Scene Fbx Setting");
	feature("description", "just so so");
}
assetprocessor
{  
    SetAnimationCompressOptimal;
    SetMeshReadableFalse;
    SetMeshOptimizeGameObjectsTrue;
    SetMeshCompressOff;
    SetMeshImportMaterialsFalse;
	SetDirty;
	SaveAndReimport;
};