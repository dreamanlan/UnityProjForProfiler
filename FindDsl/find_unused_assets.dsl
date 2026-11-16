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
    if(asset_path.Contains(filter) && !asset_path.EndsWith(".asset") && !asset_path.EndsWith(".prefab") && !asset_path.EndsWith(".unity")){
        info = asset_path;
        1;
    }else{
        0;
    };
};