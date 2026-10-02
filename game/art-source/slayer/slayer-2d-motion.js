/* Deterministic, silent portrait timing. No audio lip-sync or skeletal rig. */
(function(root){
  function frameAt(seconds,talking){
    const t=Math.max(0,Number.isFinite(seconds)?seconds:0);
    const blinkPhase=t%4.1;
    return {blink:blinkPhase>=3.82&&blinkPhase<3.96,
      talk:!!talking&&(Math.floor(t*7)%5===0||Math.floor(t*7)%5===2)};
  }
  if(typeof module!=='undefined') module.exports={frameAt};
  root.SlayerMotion={frameAt};
})(typeof globalThis!=='undefined'?globalThis:this);
