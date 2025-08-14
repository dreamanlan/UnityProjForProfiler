input("*.tga","*.png","*.jpg","*.fbx","*.FBX","*.exr","*.mat","*.controller","*.prefab","*.asset")
{
    string("filter", "");
    float("pathwidth",240){range(20,4096);};
    feature("source", "unusedassets");
    feature("menu", "5.Unused Resources");
    feature("description", "just so so");
}
filter
{
    if(assetpath.Contains(filter) && !assetpath.EndsWith(".asset") && !assetpath.EndsWith(".prefab") && !assetpath.EndsWith(".unity")){
        info = assetpath;
        1;
    }else{
        0;
    };
};