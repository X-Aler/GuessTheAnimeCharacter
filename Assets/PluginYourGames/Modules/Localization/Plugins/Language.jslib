mergeInto(LibraryManager.library, {
GeneralLanguage_js: function ()
	{
		var returnStr = navigator.language || navigator.userLanguage || "en";
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	}
});