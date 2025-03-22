input("*.tga","*.png","*.jpg","*.exr")
{
	stringlist("filter", "");
	stringlist("notfilter", "/Select/");
	bool("sizeLE",false);
	int("maxSize",512);
	float("bias",1);
	bool("eq",true);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Texture Mipmap Bias");
	feature("description", "just so so");
}
filter
{
    $v99=0;
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
        $v0 = loadasset(assetpath);
        $v1 = $v0.width;
        $v2 = $v0.height;
        $v3 = $v0.mipMapBias;
        $v4 = gettexturesetting("iPhone");
        $v5 = gettexturesetting("Android");
        //unloadasset($v0);
        if(eq && $v3==bias || !eq && $v3<bias){
            if(sizeLE && ($v1 <= maxSize || $v2 <= maxSize) && ($v4.maxTextureSize <= maxSize || $v5.maxTextureSize <= maxSize) ||
                !sizeLE && ($v1 > maxSize || $v2 > maxSize) && ($v4.maxTextureSizesizeq > maxSize || $v5.maxTextureSize > maxSize)){
                info = "size:" + $v1 + "," + $v2 + " bias:" + $v3 + " ios_size:"+$v4.maxTextureSize+" android_size:"+$v5.maxTextureSize;
                order = $v1<$v2 ? $v2 : $v1;
                value = $v3;
                $v99=1;
            };
        };
    };
    $v99;
};