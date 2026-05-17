window.terminalResize = {
    _dotnet: null,
    _onMouseMove: null,
    _onMouseUp: null,

    start: function (dotnetRef, startY, startHeight) {
        this._dotnet = dotnetRef;

        this._onMouseMove = (e) => {
            const delta = startY - e.clientY;
            const newHeight = Math.min(Math.max(startHeight + delta, 150), 800);
            dotnetRef.invokeMethodAsync('SetHeight', newHeight);
        };

        this._onMouseUp = () => {
            document.removeEventListener('mousemove', this._onMouseMove);
            document.removeEventListener('mouseup', this._onMouseUp);
            this._dotnet = null;
        };

        document.addEventListener('mousemove', this._onMouseMove);
        document.addEventListener('mouseup', this._onMouseUp);
    }
};
