/// <reference path="../../typings/index.d.ts"/>

var templateFromUrlLoader = {
	loadTemplate: function (name, templateConfig, callback) {
		if (templateConfig.fromUrl) {
			var fullUrl = '/_layouts/15/SPD2016/templates/' + templateConfig.fromUrl + '?cacheAge=' + templateConfig.maxCacheAge;
			$.get(fullUrl, function(markupString){
				ko.components.defaultLoader.loadTemplate(name, markupString, callback)
			});
		} else {
			callback(null);
		}
	}
};
ko.components.loaders.unshift(templateFromUrlLoader);