import React, { useContext, useEffect, useState } from "react";
import NavigationContext from "../contexts/navigationContext";
import { NextRouter, useRouter } from "next/router";
import { Server } from "../models/server";
import NavigationType from "../contexts/navigationType";
import CredentialsService from "../services/credentialsService";
import Well from "../models/well";
import LogObjectService from "../services/logObjectService";
import RigService from "../services/rigService";
import TrajectoryService from "../services/trajectoryService";
import BhaRunService from "../services/bhaRunService";
import { truncateAbortHandler } from "../services/apiClient";
import Wellbore, { calculateTubularGroupId } from "../models/wellbore";
import LogObject from "../models/logObject";
import {
  NavigationState,
  SelectLogObjectAction,
  SelectServerAction,
  SelectTubularAction,
  SelectWellAction,
  SelectWellboreAction,
  SetFilterAction
} from "../contexts/navigationStateReducer";
import MessageObjectService from "../services/messageObjectService";
import RiskObjectService from "../services/riskObjectService";
import TubularService from "../services/tubularService";
import Tubular from "../models/tubular";
import WbGeometryObjectService from "../services/wbGeometryService";

const Routing = (): React.ReactElement => {
  const { dispatchNavigation, navigationState } = useContext(NavigationContext);
  const { selectedServer, servers, wells, selectedWell, selectedWellbore, selectedLog, selectedTubular } = navigationState;
  const router = useRouter();
  const [isSyncingUrlAndState, setIsSyncingUrlAndState] = useState<boolean>(true);
  const [queryParamsFromUrl, setQueryParamsFromUrl] = useState<QueryParams>(null);
  const [currentQueryParams, setCurrentQueryParams] = useState<QueryParams>(null);
  useEffect(() => {
    if (isSyncingUrlAndState && (router.asPath === "/" || router.query?.serverUrl)) {
      setQueryParamsFromUrl(getQueryParamsFromUrl(router));
      setCurrentQueryParams(getQueryParamsFromState(navigationState));
    }
  }, [router]);

  useEffect(() => {
    setCurrentQueryParams(getQueryParamsFromState(navigationState));
    const finishedSyncingStateAndUrl = isSyncingUrlAndState && queryParamsFromUrl && isQueryParamsEqual(queryParamsFromUrl, currentQueryParams);
    if (finishedSyncingStateAndUrl) {
      setIsSyncingUrlAndState(false);
    }
  }, [selectedServer, selectedWell, selectedWellbore, selectedLog, selectedTubular]);

  useEffect(() => {
    if (!isSyncingUrlAndState) {
      router.push({
        pathname: "/",
        query: { ...currentQueryParams }
      });
    }
  }, [currentQueryParams, isSyncingUrlAndState]);

  useEffect(() => {
    if (isSyncingUrlAndState && queryParamsFromUrl) {
      const serverUrl = router.query.serverUrl;
      const server = servers.find((server: Server) => server.url === serverUrl);
      if (server && !selectedServer) {
        const action: SelectServerAction = { type: NavigationType.SelectServer, payload: { server } };
        dispatchNavigation(action);
        CredentialsService.setSelectedServer(server);
      }
    }
  }, [servers, queryParamsFromUrl]);

  useEffect(() => {
    if (isSyncingUrlAndState) {
      const wellUid = router.query.wellUid;
      if (wellUid && !selectedWell && wells.length > 0) {
        const well: Well = wells.find((w: Well) => w.uid === wellUid);
        if (well) {
          const selectWellAction: SelectWellAction = { type: NavigationType.SelectWell, payload: { well, wellbores: well.wellbores } };
          dispatchNavigation(selectWellAction);
        } else {
          setIsSyncingUrlAndState(false);
        }
      }
    }
  }, [wells]);

  useEffect(() => {
    if (isSyncingUrlAndState && selectedWell) {
      const setFilterAction: SetFilterAction = { type: NavigationType.SetFilter, payload: { filter: { ...navigationState.selectedFilter, wellName: selectedWell.name } } };
      dispatchNavigation(setFilterAction);
    }
  }, [selectedWell]);

  useEffect(() => {
    const wellboreUid = router.query.wellboreUid?.toString();
    const controller = new AbortController();

    async function getChildren() {
      const getBhaRuns = BhaRunService.getBhaRuns(selectedWell.uid, wellboreUid, controller.signal);
      const getLogs = LogObjectService.getLogs(selectedWell.uid, wellboreUid, controller.signal);
      const getRigs = RigService.getRigs(selectedWell.uid, wellboreUid, controller.signal);
      const getTrajectories = TrajectoryService.getTrajectories(selectedWell.uid, wellboreUid, controller.signal);
      const getTubulars = TubularService.getTubulars(selectedWell.uid, wellboreUid, controller.signal);
      const getMessages = MessageObjectService.getMessages(selectedWell.uid, wellboreUid, controller.signal);
      const getRisks = RiskObjectService.getRisks(selectedWell.uid, wellboreUid, controller.signal);
      const getWbGeometrys = WbGeometryObjectService.getWbGeometrys(selectedWell.uid, wellboreUid, controller.signal);
      const [bhaRuns, logs, rigs, trajectories, messages, risks, tubulars, wbGeometrys] = await Promise.all([
        getBhaRuns,
        getLogs,
        getRigs,
        getTrajectories,
        getMessages,
        getRisks,
        getTubulars,
        getWbGeometrys
      ]);
      const wellbore: Wellbore = selectedWell.wellbores.find((wb: Wellbore) => wb.uid === wellboreUid);
      if (wellbore) {
        const selectWellbore: SelectWellboreAction = {
          type: NavigationType.SelectWellbore,
          payload: { well: selectedWell, wellbore, bhaRuns, logs, rigs, trajectories, messages, risks, tubulars, wbGeometrys }
        } as SelectWellboreAction;
        dispatchNavigation(selectWellbore);
      } else {
        setIsSyncingUrlAndState(false);
      }
    }

    const shouldNavigateToWellbore = isSyncingUrlAndState && selectedWell && wellboreUid && !selectedWellbore;
    if (shouldNavigateToWellbore) {
      getChildren().catch(truncateAbortHandler);
      return () => {
        controller.abort();
      };
    }
  }, [selectedWell]);

  useEffect(() => {
    if (isSyncingUrlAndState) {
      const logObjectUid = router.query.logObjectUid?.toString();
      if (selectedWellbore && logObjectUid && !selectedLog) {
        const log = selectedWellbore.logs.find((l: LogObject) => l.uid === logObjectUid);
        if (log) {
          const selectLogObjectAction: SelectLogObjectAction = { type: NavigationType.SelectLogObject, payload: { log, well: selectedWell, wellbore: selectedWellbore } };
          dispatchNavigation(selectLogObjectAction);
        } else {
          setIsSyncingUrlAndState(false);
        }
      }
      const tubularUid = router.query.tubularUid?.toString();
      if (selectedWellbore && tubularUid && !selectedTubular) {
        const tubular = selectedWellbore.tubulars.find((t: Tubular) => t.uid === tubularUid);
        if (tubular) {
          const selectTubularAction: SelectTubularAction = {
            type: NavigationType.SelectTubular,
            payload: { well: selectedWell, wellbore: selectedWellbore, tubularGroup: calculateTubularGroupId(selectedWellbore), tubular }
          };
          dispatchNavigation(selectTubularAction);
        } else {
          setIsSyncingUrlAndState(false);
        }
      }
    }
  }, [selectedWellbore]);

  return <></>;
};
const isQueryParamsEqual = (urlQp: QueryParams, stateQp: QueryParams): boolean => {
  const serverIsSynced = urlQp?.serverUrl === stateQp.serverUrl;
  const wellIdSynced = urlQp?.wellUid === stateQp.wellUid;
  const wellboreIdSynced = urlQp?.wellboreUid === stateQp.wellboreUid;
  const logObjectIdSynced = urlQp?.logObjectUid === stateQp.logObjectUid;
  const tubularIdSynced = urlQp?.tubularUid === stateQp.tubularUid;
  return serverIsSynced && wellIdSynced && wellboreIdSynced && logObjectIdSynced && tubularIdSynced;
};

const getQueryParamsFromState = (state: NavigationState): QueryParams => {
  return {
    ...(state.selectedServer && { serverUrl: state.selectedServer.url }),
    ...(state.selectedWell && { wellUid: state.selectedWell.uid }),
    ...(state.selectedWellbore && { wellboreUid: state.selectedWellbore.uid }),
    ...(state.selectedLog && { logObjectUid: state.selectedLog.uid }),
    ...(state.selectedTubular && { tubularUid: state.selectedTubular.uid })
  };
};

const getQueryParamsFromUrl = (router: NextRouter): QueryParams => {
  return {
    ...(router.query.serverUrl && { serverUrl: router.query.serverUrl.toString() }),
    ...(router.query.wellUid && { wellUid: router.query.wellUid.toString() }),
    ...(router.query.wellboreUid && { wellboreUid: router.query.wellboreUid.toString() }),
    ...(router.query.logObjectUid && { logObjectUid: router.query.logObjectUid.toString() }),
    ...(router.query.tubularUid && { tubularUid: router.query.tubularUid.toString() })
  };
};

interface QueryParams {
  serverUrl: string;
  wellUid?: string;
  wellboreUid?: string;
  logObjectUid?: string;
  tubularUid?: string;
}

export default Routing;
