﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorPanzoom
{
    public class Panzoom : ComponentBase, IPanzoom, IAsyncDisposable
    {
        private HashSet<ElementReference>? _excludedElements = null;
        private PanzoomInterop _underlyingPanzoomInterop = null!;
        [Inject] private IPanzoomHelper PanzoomHelper { get; set; } = null!;

        public ElementReference ElementReference { private get; set; }

        public ElementReference ExcludedElementReference
        {
            set => AddToExcludedElements(value);
        }

        [Parameter] public bool UseParentForWheelEvents { get; set; } = true;
        [Parameter] public WheelMode WheelMode { get; set; } = WheelMode.None;
        [Parameter] public EventCallback<WheelEventArgs> OnWheel { get; set; }
        [Parameter] public PanzoomOptions PanzoomOptions { private get; set; } = PanzoomOptions.DefaultOptions;
        [Parameter] public RenderFragment<Panzoom>? ChildContent { get; set; }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await _underlyingPanzoomInterop.DisposeAsync();
        }

        public async ValueTask ZoomInAsync()
        {
            await _underlyingPanzoomInterop.ZoomInAsync();
        }

        public async ValueTask ZoomOutAsync()
        {
            await _underlyingPanzoomInterop.ZoomOutAsync();
        }

        public async ValueTask ZoomAsync(double toScale)
        {
            await _underlyingPanzoomInterop.ZoomAsync(toScale);
        }

        public async ValueTask ZoomToPointAsync(double toScale, double clientX, double clientY,
            IZoomOnlyOptions? overridenZoomOptions = default)
        {
            await _underlyingPanzoomInterop.ZoomToPointAsync(toScale, clientX, clientY, overridenZoomOptions);
        }

        public async ValueTask ResetAsync(PanzoomOptions resetOptions)
        {
            await _underlyingPanzoomInterop.ResetAsync(resetOptions);
        }

        public async ValueTask ResetAsync()
        {
            await _underlyingPanzoomInterop.ResetAsync();
        }

        public async ValueTask SetOptionsAsync(PanzoomOptions options)
        {
            await _underlyingPanzoomInterop.SetOptionsAsync(options);
        }

        public async ValueTask<PanzoomOptions> GetOptionsAsync()
        {
            return await _underlyingPanzoomInterop.GetOptionsAsync();
        }

        public async ValueTask<double> GetScaleAsync()
        {
            return await _underlyingPanzoomInterop.GetScaleAsync();
        }

        public async ValueTask DestroyAsync()
        {
            await _underlyingPanzoomInterop.DestroyAsync();
        }

        public async ValueTask ZoomWithWheel(WheelEventArgs args, IZoomOnlyOptions? overridenOptions = default)
        {
            await _underlyingPanzoomInterop.ZoomWithWheel(args, overridenOptions);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (ChildContent is null)
            {
                return;
            }

            base.BuildRenderTree(builder);
            builder.AddContent(0, ChildContent(this));
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (ElementReference.IsDefault())
                {
                    throw new ArgumentException("ElementReference must be set for Panzoom to work");
                }

                _underlyingPanzoomInterop =
                    (PanzoomInterop) await PanzoomHelper.CreateForElementReference(ElementReference, PanzoomOptions);

                if (WheelMode.Equals(WheelMode.Custom))
                {
                    if (!OnWheel.HasDelegate)
                    {
                        throw new ArgumentException("OnWheel must be set when using WheelMode.Custom!");
                    }

                    await PanzoomHelper.RegisterWheelListener(_underlyingPanzoomInterop, OnWheel, ElementReference);
                }
                else if (WheelMode.Equals(WheelMode.ZoomWithWheel))
                {
                    await PanzoomHelper.RegisterZoomWithWheel(_underlyingPanzoomInterop, ElementReference);
                }

                await UpdateExcludedElements();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task UpdateExcludedElements()
        {
            if (_excludedElements is null)
            {
                return;
            }

            if (_excludedElements.Count == 0)
            {
                return;
            }

            // 1. Get the current excluded elements from JS
            // 2. Get the new excluded elements set by the user
            // 3. Combine the current and new excluded elements
            // 4. Send the combined array to JS
            var currentOptions = await GetOptionsAsync();
            var excludedElements = currentOptions.GetExcludeOrDefault();
            var newExcludedElements = new ElementReference[excludedElements.Length + _excludedElements.Count];
            excludedElements.CopyTo(newExcludedElements, 0);
            _excludedElements.CopyTo(newExcludedElements, excludedElements.Length);
            await SetOptionsAsync(new PanzoomOptions {Exclude = newExcludedElements});
            // TODO is this the best way?
        }

        private void AddToExcludedElements(ElementReference reference)
        {
            _excludedElements ??= new HashSet<ElementReference>();
            _excludedElements.Add(reference);
        }
    }
}