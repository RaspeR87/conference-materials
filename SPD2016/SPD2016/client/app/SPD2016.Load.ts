/// <reference path="../../typings/index.d.ts"/>

import {SPD2016Model} from './models/SPD2016Model'

$(document).ready(function() {
	ExecuteOrDelayUntilScriptLoaded(function() {
		try {
			if ($("spd2016").length > 0) {
				var model = new SPD2016Model();
				ko.components.register('spd2016', {
					template: { fromUrl: 'SPD2016.html', maxCacheAge: 1234 },
					viewModel: { instance: model }
				});
				ko.applyBindings();
				model.initialize();
			}
		}
		catch (e) {
			console.log(e);
		}
	}, "sp.js");
});