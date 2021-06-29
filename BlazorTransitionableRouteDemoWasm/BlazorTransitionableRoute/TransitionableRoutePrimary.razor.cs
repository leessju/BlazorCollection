﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("BlazorTransitionableRouteTest")]

namespace BlazorTransitionableRoute
{
    /// <summary>
    /// <para>
    /// Used in the App razor file within a common Layout component
    /// and is the first recipient of the found context routeData.
    /// It must be accompanied by the <see cref="TransitionableRouteSecondary"/>
    /// as a sibling in the same manner.
    /// </para>
    /// <para>
    /// These two components alternate active views to keep
    /// component renders alive during transitions.
    /// </para>
    /// <para>
    /// Note: On first render,the primary component is active, and has
    /// a not null RouteData object. Your transition behaviour implementation
    /// can take account of the <see cref="Transition.FirstRender"/> value to adjust
    /// behaviour on first render. i.e. to transition or not to transition.
    /// </para>
    /// <para>
    /// The <see cref="Navigate(bool)"/> method is JSInvokable and triggered from
    /// JSInterop when browser page location changes.  This method was chosen over the
    /// Blazor <see cref="NavigationManager.LocationChanged"/> due to being able to also
    /// respond to browser navigation forward and back which is not currently supported.
    /// </para>
    /// <para>
    /// Usage: Add a <see cref="TransitionableRouteView"/> as a child of this 
    /// with the DefaultView that implements or inherits the
    /// <see cref="ITransitionableLayoutComponent"/>
    /// </para>
    /// </summary>
    public partial class TransitionableRoutePrimary : ComponentBase
    {
        internal bool invokesStateChanged = true;

        private bool isActive = true;
        private RouteData lastRouteData;
        public TransitionableRoutePrimary()
        {
            isActive = true;
        }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Inject]
        public IRouteTransitionInvoker TransitionInvoker { get; set; }

        internal void MakeSecondary()
        {
            isActive = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await HandleFirstRender();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        internal async Task HandleFirstRender()
        {
            await JSRuntime.InvokeVoidAsync(
                "window.blazorTransitionableRoute.init",
                DotNetObjectReference.Create(this), isActive);

            await Navigate(backwards: false, firstRender: true);
        }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RouteData RouteData { get; set; }

        [Parameter]
        public Transition Transition { get; set; }

        [Parameter]
        public bool ForgetStateOnTransition { get; set; } = false;

        [Parameter]
        public int TransitionDurationMilliseconds { get; set; } = 1000;

        [JSInvokable]
        public async Task Navigate(bool backwards)
        {
            await Navigate(backwards, firstRender: false);
        }

        internal async Task Navigate(bool backwards, bool firstRender)
        {
            var routeDataToUse = isActive ? RouteData : lastRouteData;

            var switchedRouteData = (isActive ? lastRouteData : RouteData) ?? RouteData;

            Transition = Transition.Create(routeDataToUse, switchedRouteData, isActive, backwards, firstRender);

            if (invokesStateChanged)
            {
                StateHasChanged();
            }

            var canResetStateOnTransitionOut = ForgetStateOnTransition && !isActive;

            await Task.Yield();

            if (isActive)
            {
                await TransitionInvoker.InvokeRouteTransitionAsync(Transition);
            }

            isActive = !isActive;
            lastRouteData = RouteData;

            if (canResetStateOnTransitionOut)
            {
                Transition = Transition.Create(routeData:null, switchedRouteData:null, Transition.IntoView, Transition.Backwards, Transition.FirstRender);
                await Task.Delay(TransitionDurationMilliseconds);
                if (invokesStateChanged)
                {
                    StateHasChanged();
                }
            }
        }
    }
}
