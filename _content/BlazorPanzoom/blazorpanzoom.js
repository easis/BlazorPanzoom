﻿class BlazorPanzoomInterop {

    constructor() {
    }

    createPanzoomForReference(element, options) {
        const panzoom = Panzoom(element, options)
        panzoom.boundElement = element
        return panzoom
    }

    createPanzoomForSelector(selector, options) {
        try {
            const elements = document.querySelectorAll(selector)
            const array = []
            for (let i = 0; i < elements.length; i++) {
                const element = elements[i]
                array.push(DotNet.createJSObjectReference(this.createPanzoomForReference(element, options)))
            }
            return array
        } catch {
            throw new Error(`Cannot create a Panzoom object from selectors!`);
        }
    }

    performForAllPanzoom(functionName, panzoomList, args) {
        if (!panzoomList) {
            return
        }

        for (let i = 0; i < panzoomList.length; i++) {
            const ref = panzoomList[i]
            ref[functionName](args)
        }
    }

    registerZoomWithWheel(panzoom, element) {
        const parent = element ? element.parentElement : panzoom.boundElement.parentElement
        parent.addEventListener('wheel', panzoom.zoomWithWheel)
    }

    registerWheelListener(dotnetReference, panzoom, element) {
        const parent = element ? element.parentElement : panzoom.boundElement.parentElement
        panzoom.dotNetWheelListenerReference = dotnetReference
        parent.addEventListener('wheel', panzoom.boundWheelListener = this.wheelHandler.bind(this, dotnetReference))
    }

    wheelHandler(dotnetReference, event) {
        event.preventDefault()
        dotnetReference.invokeMethodAsync('OnCustomWheelEvent', {
            deltaX: event.deltaX,
            deltaY: event.deltaY,
            clientX: event.clientX,
            clientY: event.clientY,
            shiftKey: event.shiftKey,
            altKey: event.altKey
        })
    }

    removeZoomWithWheel(panzoom, element) {
        const parent = element ? element.parentElement : panzoom.boundElement.parentElement
        parent.removeEventListener('wheel', panzoom.zoomWithWheel);
    }

    removeWheelListener(panzoom, element) {
        const parent = element ? element.parentElement : panzoom.boundElement.parentElement
        if (panzoom.boundWheelListener) {
            parent.removeEventListener('wheel', panzoom.boundWheelListener);
            panzoom.dotNetWheelListenerReference.dispose()
            delete panzoom.dotNetWheelListenerReference
            delete panzoom.boundWheelListener
        }
    }

    destroyPanzoom(panzoom) {
        if (panzoom) {
            delete panzoom.boundElement
        }
    }
}

window.blazorPanzoom = new BlazorPanzoomInterop()